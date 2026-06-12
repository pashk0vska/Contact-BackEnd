using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin,master")]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(AppDbContext db, ILogger<ClientsController> logger)
        {
            _db = db; _logger = logger;
        }

        // FromConfigurator = true, якщо клієнта створено в Конфігураторі ПК (Source == "configurator").
        public record ClientListItemDto(int Id, string FullName, string Phone, string Email, bool FromConfigurator);

        // GET — всі ролі
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q,
            [FromQuery] string sort = "FullName", [FromQuery] string dir = "asc",
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 10;

            IQueryable<Client> query = _db.Clients.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    EF.Functions.Like(c.FullName.ToLower(), $"%{term}%") ||
                    EF.Functions.Like(c.Phone.ToLower(), $"%{term}%") ||
                    EF.Functions.Like(c.Email.ToLower(), $"%{term}%"));
            }

            bool desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "phone" => desc ? query.OrderByDescending(x => x.Phone).ThenBy(x => x.Id)    : query.OrderBy(x => x.Phone).ThenBy(x => x.Id),
                "email" => desc ? query.OrderByDescending(x => x.Email).ThenBy(x => x.Id)    : query.OrderBy(x => x.Email).ThenBy(x => x.Id),
                _       => desc ? query.OrderByDescending(x => x.FullName).ThenBy(x => x.Id) : query.OrderBy(x => x.FullName).ThenBy(x => x.Id)
            };

            var total = await query.CountAsync();
            // FromConfigurator читаємо з тієї ж вибірки — без додаткового запиту до БД.
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new ClientListItemDto(c.Id, c.FullName, c.Phone, c.Email, c.Source == "configurator"))
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }

        // POST — всі ролі (email тепер обов'язковий)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Client model)
        {
            if (model == null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(model.FullName)) return BadRequest("FullName is required.");
            if (string.IsNullOrWhiteSpace(model.Phone)) return BadRequest("Phone is required.");
            if (string.IsNullOrWhiteSpace(model.Email)) return BadRequest("Email is required.");

            model.Email = model.Email.Trim();
            model.Source = "crm";   // клієнт, створений у CRM (Конфігуратор виставляє "configurator" сам)
            _db.Clients.Add(model);
            await _db.SaveChangesAsync();
            return Ok(new { id = model.Id });
        }

        // PUT — всі ролі (email тепер обов'язковий)
        // Source НЕ чіпаємо — редагування в CRM не змінює походження клієнта.
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] Client model)
        {
            var entity = await _db.Clients.FindAsync(id);
            if (entity == null) return NotFound();
            if (string.IsNullOrWhiteSpace(model.FullName)) return BadRequest("FullName is required.");
            if (string.IsNullOrWhiteSpace(model.Phone)) return BadRequest("Phone is required.");
            if (string.IsNullOrWhiteSpace(model.Email)) return BadRequest("Email is required.");

            entity.FullName = model.FullName.Trim();
            entity.Phone    = model.Phone.Trim();
            entity.Email    = model.Email.Trim();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Каскадне видалення пов'язаних із клієнтом записів:
        // • позиції продажів (sale_items) проданих цьому клієнту;
        // • продажі (sale_headers) клієнта (FK на клієнта — Restrict, тому прибираємо вручну);
        // • ремонти клієнта (інакше лишались «осиротілі» ремонти з порожньою колонкою клієнта).
        private async Task DeleteClientCascadeAsync(IEnumerable<int> clientIds)
        {
            var ids = clientIds.Distinct().ToList();
            if (ids.Count == 0) return;

            var saleIds = await _db.SaleHeaders.Where(h => ids.Contains(h.ClientId)).Select(h => h.Id).ToListAsync();
            if (saleIds.Count > 0)
            {
                _db.SaleItems.RemoveRange(_db.SaleItems.Where(i => saleIds.Contains(i.SaleId)));
                _db.SaleHeaders.RemoveRange(_db.SaleHeaders.Where(h => ids.Contains(h.ClientId)));
            }
            _db.Repairs.RemoveRange(_db.Repairs.Where(r => ids.Contains(r.ClientId)));
            _db.Clients.RemoveRange(_db.Clients.Where(c => ids.Contains(c.Id)));
            await _db.SaveChangesAsync();
        }

        // DELETE — тільки superadmin та admin (master НЕ може видаляти)
        // Видаляє клієнта РАЗОМ з його ремонтами та продажами (каскадно).
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var entity = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await DeleteClientCascadeAsync(new[] { id });
                await tx.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Delete client {Id} failed", id);
                return StatusCode(500, "Не вдалося видалити клієнта.");
            }
        }

        // batch-delete — тільки superadmin та admin (теж каскадно)
        [HttpPost("batch-delete")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> BatchDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("No ids provided");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var existing = await _db.Clients.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync();
                await DeleteClientCascadeAsync(existing);
                await tx.CommitAsync();
                _logger.LogInformation("Batch deleted {Count} clients (with related repairs/sales)", existing.Count);
                return Ok(new { deleted = existing.Count });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Batch delete clients failed");
                return StatusCode(500, "Не вдалося видалити вибраних клієнтів.");
            }
        }

        // GET history — всі ролі
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound();

            var repairs = await (from r in _db.Repairs.AsNoTracking()
                                where r.ClientId == id
                                select new
                                {
                                    date    = r.CreatedAt,
                                    device  = (r.DeviceType ?? "") + (string.IsNullOrWhiteSpace(r.Model) ? "" : " " + r.Model),
                                    problem = r.Problem,
                                    status  = r.Status,
                                    price   = r.TotalCost
                                }).ToListAsync();

            var sales = await (from h in _db.SaleHeaders.AsNoTracking()
                              where h.ClientId == id
                              join i in _db.SaleItems.AsNoTracking() on h.Id equals i.SaleId into gi
                              select new
                              {
                                  date    = h.Date,
                                  product = gi.Select(x => x.Name).FirstOrDefault() ?? "",
                                  total   = h.Total,
                                  status  = h.Status ?? ""
                              }).ToListAsync();

            return Ok(new { clientName = client.FullName, repairs, sales });
        }
    }
}
