using System;
using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Contact.API.Helpers;
using Contact.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RepairsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RepairsController> _logger;

        public RepairsController(AppDbContext db, ILogger<RepairsController> logger)
        {
            _db = db;
            _logger = logger;
        }

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

        [HttpGet]
        public async Task<IActionResult> GetRepairs([FromQuery] RepairsQuery rq)
        {
            _logger.LogInformation("Отримання списку ремонтів. Сторінка: {Page}", rq.page);

            var q0 =
                from r in _db.Repairs.AsNoTracking()
                join c0 in _db.Clients.AsNoTracking() on r.ClientId equals c0.Id into gc
                from c in gc.DefaultIfEmpty()
                select new
                {
                    r.Id,
                    Date = r.CreatedAt,
                    Device = (r.DeviceType ?? "") +
                             (string.IsNullOrWhiteSpace(r.Model) ? "" : " " + r.Model),
                    r.Problem,
                    r.Status,
                    Price = r.TotalCost,
                    ClientName = c != null ? c.FullName : ""
                };

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

            if (rq.from.HasValue) q0 = q0.Where(x => x.Date >= DateTimeHelper.NormalizeToUtc(rq.from.Value));
            if (rq.to.HasValue) q0 = q0.Where(x => x.Date <= DateTimeHelper.NormalizeToUtc(rq.to.Value));
            if (!string.IsNullOrWhiteSpace(rq.status))
            {
                var s = rq.status.Trim().ToLower();
                q0 = q0.Where(x => (x.Status ?? "").ToLower() == s);
            }
            if (!string.IsNullOrWhiteSpace(rq.deviceType))
            {
                var d = rq.deviceType.Trim().ToLower();
                q0 = q0.Where(x => (x.Device ?? "").ToLower().Contains(d));
            }

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

        public class RepairCreateDto
        {
            public int? ClientId { get; set; }
            public string? ClientName { get; set; }
            public DateTime Date { get; set; }
            public string Device { get; set; } = "";
            public string Problem { get; set; } = "";
            public string Status { get; set; } = "new";
            public decimal Price { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRepair([FromBody] RepairCreateDto dto)
        {
            if (dto == null) return BadRequest("Empty payload");
            if (string.IsNullOrWhiteSpace(dto.Device)) return BadRequest("Device is required");
            if (string.IsNullOrWhiteSpace(dto.Problem)) return BadRequest("Problem is required");

            var clientResult = await ClientResolver.ResolveOrCreateAsync(_db, dto.ClientId, dto.ClientName);
            if (!clientResult.Success) return BadRequest(clientResult.ErrorMessage);

            var r = new Repair
            {
                ClientId = clientResult.ClientId,
                CreatedAt = DateTimeHelper.NormalizeOrNow(dto.Date),
                DeviceType = dto.Device,
                Model = "",
                Problem = dto.Problem,
                Status = dto.Status ?? "new",
                PartsUsed = "",
                TotalCost = dto.Price
            };

            _db.Repairs.Add(r);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Створено ремонт. Id: {Id}, Пристрій: {Device}", r.Id, r.DeviceType);
            return Ok(new { id = r.Id });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRepair(int id)
        {
            var r = await _db.Repairs.FirstOrDefaultAsync(x => x.Id == id);
            if (r == null)
            {
                _logger.LogWarning("Ремонт не знайдено для видалення. Id: {Id}", id);
                return NotFound();
            }

            _db.Repairs.Remove(r);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Видалено ремонт. Id: {Id}", id);
            return NoContent();
        }
    }
}