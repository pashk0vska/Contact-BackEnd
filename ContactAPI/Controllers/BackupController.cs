using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using Contact.API.Models;
using System.IO;
using System.Text.Json;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin")]
    public class BackupController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BackupController> _logger;
        public BackupController(AppDbContext db, ILogger<BackupController> logger)
        {
            _db = db; _logger = logger;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var data = new
            {
                clients     = await _db.Clients.AsNoTracking().ToListAsync(),
                repairs     = await _db.Repairs.AsNoTracking().ToListAsync(),
                saleHeaders = await _db.SaleHeaders.AsNoTracking().ToListAsync(),
                saleItems   = await _db.SaleItems.AsNoTracking().ToListAsync(),
                services    = await _db.Services.AsNoTracking().ToListAsync(),
                users       = await _db.Users.AsNoTracking().ToListAsync(),
                exportDate  = DateTime.UtcNow
            };
            var json  = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", "kontakt_backup.json");
        }

        public class BackupData
        {
            public List<Client>?     clients     { get; set; }
            public List<Repair>?     repairs     { get; set; }
            public List<SaleHeader>? saleHeaders { get; set; }
            public List<SaleItem>?   saleItems   { get; set; }
            public List<Service>?    services    { get; set; }
            public List<User>?       users       { get; set; }
        }

        // Import — тільки superadmin (повне відновлення бази — дуже критична операція)
        // ВАЖЛИВО:
        // • Тіло читаємо вручну (без [FromBody]) — щоб не спрацьовувала валідація моделей
        // ([Required] на Repair.Model / PartsUsed тощо давала 400 на порожніх рядках).
        // • [DisableRequestSizeLimit] — щоб великий бекап не впирався в 413.
        // • Id перемапуються: вставляємо записи з Id=0 (БД присвоює нові), а зв'язки
        // (ClientId у ремонтах/продажах, SaleId у позиціях) переписуємо на нові Id.
        // • Користувачів НЕ чіпаємо — щоб не зламати поточну сесію адміністратора.
        [HttpPost("import")]
        [Authorize(Roles = "superadmin")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Import()
        {
            string json;
            using (var reader = new StreamReader(Request.Body))
                json = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(json)) return BadRequest("No data");

            BackupData? data;
            try
            {
                data = JsonSerializer.Deserialize<BackupData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid backup JSON: {ex.Message}");
            }
            if (data == null) return BadRequest("No data");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Чистимо поточні дані (КРІМ користувачів). Порядок важливий через FK.
                _db.SaleItems.RemoveRange(_db.SaleItems);
                _db.SaleHeaders.RemoveRange(_db.SaleHeaders);
                _db.Repairs.RemoveRange(_db.Repairs);
                _db.Services.RemoveRange(_db.Services);
                _db.Clients.RemoveRange(_db.Clients);
                await _db.SaveChangesAsync();

                // 2) Клієнти (з перемапінгом Id старий -> новий)
                var clientIdMap = new Dictionary<int, int>();
                if (data.clients != null && data.clients.Count > 0)
                {
                    var oldIds = data.clients.Select(c => c.Id).ToList();
                    foreach (var c in data.clients) c.Id = 0;
                    _db.Clients.AddRange(data.clients);
                    await _db.SaveChangesAsync();
                    for (int i = 0; i < data.clients.Count; i++) clientIdMap[oldIds[i]] = data.clients[i].Id;
                }

                // 3) Послуги
                if (data.services != null && data.services.Count > 0)
                {
                    foreach (var s in data.services) s.Id = 0;
                    _db.Services.AddRange(data.services);
                    await _db.SaveChangesAsync();
                }

                // 4) Ремонти (перемапінг ClientId)
                if (data.repairs != null && data.repairs.Count > 0)
                {
                    foreach (var r in data.repairs)
                    {
                        r.Id = 0;
                        if (clientIdMap.TryGetValue(r.ClientId, out var nid)) r.ClientId = nid;
                    }
                    _db.Repairs.AddRange(data.repairs);
                    await _db.SaveChangesAsync();
                }

                // 5) Продажі (перемапінг ClientId) + збираємо мапу Id заголовків
                var headerIdMap = new Dictionary<int, int>();
                if (data.saleHeaders != null && data.saleHeaders.Count > 0)
                {
                    var oldIds = data.saleHeaders.Select(h => h.Id).ToList();
                    foreach (var h in data.saleHeaders)
                    {
                        h.Id = 0;
                        h.Items = new();
                        if (clientIdMap.TryGetValue(h.ClientId, out var nid)) h.ClientId = nid;
                    }
                    _db.SaleHeaders.AddRange(data.saleHeaders);
                    await _db.SaveChangesAsync();
                    for (int i = 0; i < data.saleHeaders.Count; i++) headerIdMap[oldIds[i]] = data.saleHeaders[i].Id;
                }

                // 6) Позиції продажів (перемапінг SaleId)
                if (data.saleItems != null && data.saleItems.Count > 0)
                {
                    foreach (var it in data.saleItems)
                    {
                        it.Id = 0;
                        it.Sale = null;
                        if (headerIdMap.TryGetValue(it.SaleId, out var nid)) it.SaleId = nid;
                    }
                    _db.SaleItems.AddRange(data.saleItems);
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Backup restored: {C} clients, {R} repairs, {H} sales",
                    data.clients?.Count ?? 0, data.repairs?.Count ?? 0, data.saleHeaders?.Count ?? 0);
                return Ok(new { message = "Restored successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Restore failed");
                return StatusCode(500, $"Restore failed: {ex.Message}");
            }
        }
    }
}
