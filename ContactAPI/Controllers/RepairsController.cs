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
    [Authorize(Roles = "superadmin,admin,master")]
    public class RepairsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RepairsController> _logger;

        public RepairsController(AppDbContext db, ILogger<RepairsController> logger)
        {
            _db = db; _logger = logger;
        }

        public record RepairsQuery(string? q, string? sort = "Date", string? dir = "desc",
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
            public string? ClientPhone { get; set; }
            public int? MasterId { get; set; }
            public string? MasterName { get; set; }
        }

        // GET /api/repairs — всі ролі
        [HttpGet]
        public async Task<IActionResult> GetRepairs([FromQuery] RepairsQuery rq)
        {
            var q0 = from r in _db.Repairs.AsNoTracking()
                     join c0 in _db.Clients.AsNoTracking() on r.ClientId equals c0.Id into gc
                     from c in gc.DefaultIfEmpty()
                     select new
                     {
                         r.Id,
                         Date   = r.CreatedAt,
                         Device = (r.DeviceType ?? "") + (string.IsNullOrWhiteSpace(r.Model) ? "" : " " + r.Model),
                         r.Problem, r.Status,
                         Price      = r.TotalCost,
                         ClientName = c != null ? c.FullName : "",
                         r.MasterId
                     };

            if (!string.IsNullOrWhiteSpace(rq.q))
            {
                var q = rq.q.Trim().ToLower();
                q0 = q0.Where(x => (x.ClientName ?? "").ToLower().Contains(q)
                                || (x.Device ?? "").ToLower().Contains(q)
                                || (x.Problem ?? "").ToLower().Contains(q)
                                || x.Id.ToString() == q);
            }
            if (rq.from.HasValue)       q0 = q0.Where(x => x.Date >= DateTimeHelper.NormalizeToUtc(rq.from.Value));
            if (rq.to.HasValue)         q0 = q0.Where(x => x.Date <= DateTimeHelper.NormalizeToUtc(rq.to.Value));
            if (!string.IsNullOrWhiteSpace(rq.status))     { var s = rq.status.Trim().ToLower();     q0 = q0.Where(x => (x.Status ?? "").ToLower() == s); }
            if (!string.IsNullOrWhiteSpace(rq.deviceType)) { var d = rq.deviceType.Trim().ToLower(); q0 = q0.Where(x => (x.Device ?? "").ToLower().Contains(d)); }

            bool desc = string.Equals(rq.dir, "desc", StringComparison.OrdinalIgnoreCase);
            q0 = (rq.sort ?? "Date").ToLower() switch
            {
                "id"     => desc ? q0.OrderByDescending(x => x.Id)         : q0.OrderBy(x => x.Id),
                "client" => desc ? q0.OrderByDescending(x => x.ClientName) : q0.OrderBy(x => x.ClientName),
                "device" => desc ? q0.OrderByDescending(x => x.Device)     : q0.OrderBy(x => x.Device),
                "status" => desc ? q0.OrderByDescending(x => x.Status)     : q0.OrderBy(x => x.Status),
                "price"  => desc ? q0.OrderByDescending(x => x.Price)      : q0.OrderBy(x => x.Price),
                _        => desc ? q0.OrderByDescending(x => x.Date)       : q0.OrderBy(x => x.Date)
            };

            var total = await q0.CountAsync();
            var items = await q0.Skip((rq.page - 1) * rq.pageSize).Take(rq.pageSize)
                .Select(x => new RepairListItemDto
                {
                    Id         = x.Id,
                    Date       = x.Date,
                    ClientName = x.ClientName ?? "",
                    Device     = x.Device ?? "",
                    Problem    = x.Problem ?? "",
                    Status     = x.Status ?? "",
                    Price      = x.Price,
                    MasterId   = x.MasterId,
                    MasterName = _db.Users.Where(u => u.Id == x.MasterId).Select(u => u.Username).FirstOrDefault()
                }).ToListAsync();

            return Ok(new { items, total, page = rq.page, pageSize = rq.pageSize });
        }

        // GET /api/repairs/{id} — всі ролі (повні деталі + контакти клієнта + майстер)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRepairById(int id)
        {
            var repair = await _db.Repairs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (repair == null) return NotFound();

            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == repair.ClientId);

            string? masterName = repair.MasterId == null ? null
                : await _db.Users.AsNoTracking().Where(u => u.Id == repair.MasterId)
                    .Select(u => u.Username).FirstOrDefaultAsync();

            return Ok(new
            {
                repair.Id,
                repair.Status,
                repair.DeviceType,
                repair.Model,
                repair.Problem,
                repair.PartsUsed,
                repair.TotalCost,
                createdAt  = repair.CreatedAt,
                Date       = repair.CreatedAt,           // сумісність зі старою модалкою редагування
                ClientName = client?.FullName ?? "",     // сумісність зі старою модалкою редагування
                masterId   = repair.MasterId,
                masterName,
                client = client == null ? null : new
                {
                    client.FullName,
                    client.Phone,
                    client.Email
                }
            });
        }

        public class RepairCreateDto
        {
            public int?     ClientId    { get; set; }
            public string?  ClientName  { get; set; }
            public DateTime Date        { get; set; }
            public string   Device      { get; set; } = "";
            public string   Problem     { get; set; } = "";
            public string   Status      { get; set; } = "new";
            public decimal  Price       { get; set; }
            public string?  ClientPhone { get; set; }
            public string?  ClientEmail { get; set; }
            public int?     MasterId    { get; set; }
        }

        // POST /api/repairs — всі ролі (superadmin, admin, master)
        [HttpPost]
        public async Task<IActionResult> CreateRepair([FromBody] RepairCreateDto dto)
        {
            if (dto == null)                               return BadRequest("Empty payload");
            if (string.IsNullOrWhiteSpace(dto.Device))    return BadRequest("Device is required");
            if (string.IsNullOrWhiteSpace(dto.Problem))   return BadRequest("Problem is required");

            var cr = await ClientResolver.ResolveOrCreateAsync(_db, dto.ClientId, dto.ClientName, dto.ClientPhone, dto.ClientEmail);
            if (!cr.Success) return BadRequest(cr.ErrorMessage);

            var r = new Repair
            {
                ClientId  = cr.ClientId,
                CreatedAt = DateTimeHelper.NormalizeOrNow(dto.Date),
                DeviceType = dto.Device,
                Model      = "",
                Problem    = dto.Problem,
                Status     = dto.Status ?? "new",
                PartsUsed  = "",
                TotalCost  = dto.Price,
                MasterId   = dto.MasterId
            };

            _db.Repairs.Add(r);
            await _db.SaveChangesAsync();
            return Ok(new { id = r.Id });
        }

        // PUT /api/repairs/{id} — всі ролі (superadmin, admin, master)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateRepair(int id, [FromBody] RepairCreateDto dto)
        {
            var r = await _db.Repairs.FirstOrDefaultAsync(x => x.Id == id);
            if (r == null)                              return NotFound();
            if (string.IsNullOrWhiteSpace(dto.Device)) return BadRequest("Device is required");
            if (string.IsNullOrWhiteSpace(dto.Problem))return BadRequest("Problem is required");

            r.DeviceType = dto.Device;
            r.Problem    = dto.Problem;
            r.Status     = dto.Status ?? r.Status;
            r.TotalCost  = dto.Price;
            // майстра оновлюємо лише якщо переданий (щоб старі форми без поля не скидали його)
            if (dto.MasterId.HasValue) r.MasterId = dto.MasterId;
            if (dto.Date != default) r.CreatedAt = DateTimeHelper.NormalizeOrNow(dto.Date);

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/repairs/{id} — тільки superadmin та admin (master НЕ може видаляти)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteRepair(int id)
        {
            var r = await _db.Repairs.FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound();
            _db.Repairs.Remove(r);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/repairs/{id}/create-sale — всі ролі
        [HttpPost("{id:int}/create-sale")]
        public async Task<IActionResult> CreateSaleFromRepair(int id)
        {
            var repair = await _db.Repairs.FirstOrDefaultAsync(x => x.Id == id);
            if (repair == null) return NotFound("Ремонт не знайдено");

            var header = new SaleHeader
            {
                ClientId  = repair.ClientId,
                ServiceId = 0,
                Price     = 0,
                Date      = DateTime.UtcNow,
                Payment   = "Готівка",
                Status    = "done",
                Note      = $"Оплата за ремонт #{repair.Id}",
                Total     = repair.TotalCost,
                MasterId  = repair.MasterId
            };
            _db.SaleHeaders.Add(header);
            await _db.SaveChangesAsync();

            var item = new SaleItem
            {
                SaleId = header.Id,
                Name   = $"Ремонт: {repair.DeviceType} {repair.Model} — {repair.Problem}",
                Qty    = 1,
                Price  = repair.TotalCost
            };
            _db.SaleItems.Add(item);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created sale #{SaleId} from repair #{RepairId}", header.Id, id);
            return Ok(new { saleId = header.Id });
        }
    }
}
