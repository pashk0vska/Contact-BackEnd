using System;
using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepairsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public RepairsController(AppDbContext db) => _db = db;

        // Normalize any incoming DateTime to UTC.
        // - Utc: keep
        // - Local: convert
        // - Unspecified: treat as local (date-only inputs)
        private static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
            };
        }

        // ===== Query/DTO =====
        public record RepairsQuery(
            string? q,
            string? sort = "Date", string? dir = "desc",
            int page = 1, int pageSize = 8,
            DateTime? from = null, DateTime? to = null,
            string? status = null, string? deviceType = null);

        public class RepairListItemDto
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string ClientName { get; set; } = "";
            public string Device { get; set; } = "";
            public string Problem { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal Price { get; set; }
        }

        // ===== GET /api/Repairs =====
        [HttpGet]
        public async Task<IActionResult> GetRepairs([FromQuery] RepairsQuery rq)
        {
            var q0 =
                from r in _db.Repairs.AsNoTracking()
                join c0 in _db.Clients.AsNoTracking() on r.ClientId equals c0.Id into gc
                from c in gc.DefaultIfEmpty()
                select new
                {
                    r.Id,
                    Date = r.CreatedAt,                          // <-- правильне поле дати
                    Device = (r.DeviceType ?? "") +              // <-- склеюємо тип + модель
                             (string.IsNullOrWhiteSpace(r.Model) ? "" : " " + r.Model),
                    r.Problem,
                    r.Status,
                    Price = r.TotalCost,                          // <-- правильне поле суми
                    ClientName = c != null ? c.FullName : ""
                };

            // Пошук
            if (!string.IsNullOrWhiteSpace(rq.q))
            {
                var q = rq.q.Trim().ToLower();
                q0 = q0.Where(x =>
                    (x.ClientName ?? "").ToLower().Contains(q) ||
                    (x.Device ?? "").ToLower().Contains(q) ||
                    (x.Problem ?? "").ToLower().Contains(q) ||
                    x.Id.ToString() == q
                );
            }

            // Фільтри
            if (rq.from.HasValue) q0 = q0.Where(x => x.Date >= NormalizeToUtc(rq.from.Value));
            if (rq.to.HasValue) q0 = q0.Where(x => x.Date <= NormalizeToUtc(rq.to.Value));
            if (!string.IsNullOrWhiteSpace(rq.status))
            {
                var s = rq.status.Trim().ToLower();
                q0 = q0.Where(x => (x.Status ?? "").ToLower() == s);
            }
            if (!string.IsNullOrWhiteSpace(rq.deviceType))
            {
                var d = rq.deviceType.Trim().ToLower();
                // фільтруємо по склеєному Device (містить і DeviceType, і Model)
                q0 = q0.Where(x => (x.Device ?? "").ToLower().Contains(d));
            }

            // Сортування
            bool desc = string.Equals(rq.dir, "desc", StringComparison.OrdinalIgnoreCase);
            q0 = (rq.sort ?? "Date").ToLower() switch
            {
                "id" => desc ? q0.OrderByDescending(x => x.Id) : q0.OrderBy(x => x.Id),
                "client" => desc ? q0.OrderByDescending(x => x.ClientName) : q0.OrderBy(x => x.ClientName),
                "device" => desc ? q0.OrderByDescending(x => x.Device) : q0.OrderBy(x => x.Device),
                "status" => desc ? q0.OrderByDescending(x => x.Status) : q0.OrderBy(x => x.Status),
                "price" => desc ? q0.OrderByDescending(x => x.Price) : q0.OrderBy(x => x.Price),
                _ => desc ? q0.OrderByDescending(x => x.Date) : q0.OrderBy(x => x.Date),
            };

            var total = await q0.CountAsync();
            var items = await q0
                .Skip((rq.page - 1) * rq.pageSize)
                .Take(rq.pageSize)
                .Select(x => new RepairListItemDto
                {
                    Id = x.Id,
                    Date = x.Date,
                    ClientName = x.ClientName ?? "",
                    Device = x.Device ?? "",
                    Problem = x.Problem ?? "",
                    Status = x.Status ?? "",
                    Price = x.Price
                })
                .ToListAsync();

            return Ok(new { items, total, page = rq.page, pageSize = rq.pageSize });
        }

        // ===== DTO для створення ордера =====
        public class RepairCreateDto
        {
            public int? ClientId { get; set; }
            public string? ClientName { get; set; } // якщо ClientId немає — створимо/знайдемо за ім’ям
            public DateTime Date { get; set; }
            public string Device { get; set; } = ""; // піде в DeviceType
            public string Problem { get; set; } = "";
            public string Status { get; set; } = "new"; // new / progress / done / issued / canceled
            public decimal Price { get; set; }          // піде в TotalCost
        }

        // ===== POST /api/Repairs =====
        [HttpPost]
        public async Task<IActionResult> CreateRepair([FromBody] RepairCreateDto dto)
        {
            if (dto == null) return BadRequest("Empty payload");
            if (string.IsNullOrWhiteSpace(dto.Device)) return BadRequest("Device is required");
            if (string.IsNullOrWhiteSpace(dto.Problem)) return BadRequest("Problem is required");

            // resolve/create client
            int clientId;
            if (dto.ClientId.GetValueOrDefault() > 0)
            {
                clientId = dto.ClientId!.Value;
            }
            else
            {
                var name = (dto.ClientName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) return BadRequest("Client is required");

                var existing = await _db.Clients
                    .AsNoTracking()
                    .Where(c => c.FullName.ToLower() == name.ToLower())
                    .Select(c => new { c.Id })
                    .FirstOrDefaultAsync();

                if (existing != null) clientId = existing.Id;
                else
                {
                    var newClient = new Client { FullName = name, Phone = "", Email = "", History = "" };
                    _db.Clients.Add(newClient);
                    await _db.SaveChangesAsync();
                    clientId = newClient.Id;
                }
            }

            var r = new Repair
            {
                ClientId = clientId,
                // Зберігаємо дату в UTC. Якщо прийшла date-only/Unspecified - вважаємо локальним часом.
                CreatedAt = dto.Date == default ? DateTime.UtcNow : NormalizeToUtc(dto.Date), // ← правильне поле
                DeviceType = dto.Device,      // ← пишемо у DeviceType
                Model = "",                   // поки не збираємо окремо
                Problem = dto.Problem,
                Status = dto.Status ?? "new",
                PartsUsed = "",               // поки не збираємо
                TotalCost = dto.Price         // ← правильне поле
            };

            _db.Repairs.Add(r);
            await _db.SaveChangesAsync();
            return Ok(new { id = r.Id });
        }

        // ===== DELETE /api/Repairs/{id} =====
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRepair(int id)
        {
            var r = await _db.Repairs.FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound();
            _db.Repairs.Remove(r);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
