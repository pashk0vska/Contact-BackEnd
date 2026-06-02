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
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SalesController> _logger;

        public SalesController(AppDbContext db, ILogger<SalesController> logger)
        {
            _db = db; _logger = logger;
        }

        public record SalesQuery(string? q, string? sort = "Date", string? dir = "desc",
            int page = 1, int pageSize = 8,
            DateTime? from = null, DateTime? to = null, string? status = null);

        public class SaleListItemDto
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string ClientName { get; set; } = "";
            public string ProductName { get; set; } = "";
            public int Quantity { get; set; }
            public decimal TotalPrice { get; set; }
            public string Payment { get; set; } = "";
            public string Status { get; set; } = "";
        }

        // GET — всі ролі
        [HttpGet]
        public async Task<IActionResult> GetSales([FromQuery] SalesQuery rq)
        {
            var baseQuery = from h in _db.SaleHeaders.AsNoTracking()
                            join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc
                            from c in gc.DefaultIfEmpty()
                            join i in _db.SaleItems.AsNoTracking() on h.Id equals i.SaleId into gi
                            select new { Header = h, ClientName = c != null ? c.FullName : "", Items = gi };

            if (!string.IsNullOrWhiteSpace(rq.q))
            {
                var q = rq.q.Trim().ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.ClientName ?? "").ToLower().Contains(q) ||
                    (x.Header.Payment ?? "").ToLower().Contains(q) ||
                    (x.Header.Status ?? "").ToLower().Contains(q) ||
                    x.Items.Any(it => (it.Name ?? "").ToLower().Contains(q)));
            }
            if (rq.from.HasValue) baseQuery = baseQuery.Where(x => x.Header.Date >= DateTimeHelper.NormalizeToUtc(rq.from.Value));
            if (rq.to.HasValue)   baseQuery = baseQuery.Where(x => x.Header.Date <= DateTimeHelper.NormalizeToUtc(rq.to.Value));
            if (!string.IsNullOrWhiteSpace(rq.status))
            {
                var s = rq.status.Trim().ToLower();
                baseQuery = baseQuery.Where(x => (x.Header.Status ?? "").ToLower() == s);
            }

            var projected = baseQuery.Select(x => new SaleListItemDto
            {
                Id          = x.Header.Id,
                Date        = x.Header.Date,
                ClientName  = x.ClientName ?? "",
                ProductName = x.Items.Select(it => it.Name).FirstOrDefault() ?? "",
                Quantity    = x.Items.Select(it => (int?)it.Qty).Sum() ?? 1,
                TotalPrice  = x.Header.Total != 0 ? x.Header.Total : (x.Items.Select(it => (decimal?)it.Price * it.Qty).Sum() ?? 0m),
                Payment     = x.Header.Payment ?? "",
                Status      = x.Header.Status ?? ""
            });

            bool desc = string.Equals(rq.dir, "desc", StringComparison.OrdinalIgnoreCase);
            projected = (rq.sort ?? "Date").ToLower() switch
            {
                "clientname"  => desc ? projected.OrderByDescending(x => x.ClientName)  : projected.OrderBy(x => x.ClientName),
                "productname" => desc ? projected.OrderByDescending(x => x.ProductName) : projected.OrderBy(x => x.ProductName),
                "quantity"    => desc ? projected.OrderByDescending(x => x.Quantity)     : projected.OrderBy(x => x.Quantity),
                "totalprice"  => desc ? projected.OrderByDescending(x => x.TotalPrice)   : projected.OrderBy(x => x.TotalPrice),
                "payment"     => desc ? projected.OrderByDescending(x => x.Payment)      : projected.OrderBy(x => x.Payment),
                "status"      => desc ? projected.OrderByDescending(x => x.Status)       : projected.OrderBy(x => x.Status),
                _             => desc ? projected.OrderByDescending(x => x.Date)         : projected.OrderBy(x => x.Date)
            };

            var total = await projected.CountAsync();
            var items = await projected.Skip((rq.page - 1) * rq.pageSize).Take(rq.pageSize).ToListAsync();
            return Ok(new { items, total, page = rq.page, pageSize = rq.pageSize });
        }

        // GET {id} — всі ролі
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSaleById(int id)
        {
            var header = await _db.SaleHeaders.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (header == null) return NotFound();
            var client = await _db.Clients.FindAsync(header.ClientId);
            var items  = await _db.SaleItems.AsNoTracking().Where(i => i.SaleId == id).ToListAsync();
            return Ok(new
            {
                header.Id, header.Date, header.Payment, header.Status, header.Note, header.Total,
                ClientName = client?.FullName ?? "",
                Items      = items.Select(i => new { i.Name, i.Qty, i.Price })
            });
        }

        // GET recent — всі ролі
        [HttpGet("recent")]
        public async Task<IActionResult> Recent([FromQuery] int take = 8)
        {
            take = Math.Clamp(take, 1, 50);
            var q = from h in _db.SaleHeaders.AsNoTracking()
                    join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc
                    from c in gc.DefaultIfEmpty()
                    join i0 in _db.SaleItems.AsNoTracking() on h.Id equals i0.SaleId into gi
                    orderby h.Date descending
                    select new { name = c != null ? c.FullName : "", item = gi.Select(x => x.Name).FirstOrDefault(), price = h.Total };
            return Ok(await q.Take(take).ToListAsync());
        }

        public class SaleCreateItemDto
        {
            public string Name { get; set; } = "";
            public int Qty { get; set; }
            public decimal Price { get; set; }
        }

        public class SaleCreateDto
        {
            public int?    ClientId    { get; set; }
            public string? ClientName  { get; set; }
            public DateTime Date       { get; set; }
            public string  Payment     { get; set; } = "";
            public string  Status      { get; set; } = "done";
            public string? Note        { get; set; }
            public SaleCreateItemDto Item { get; set; } = new();
            public string? ClientPhone { get; set; }
            public bool UpsertService  { get; set; } = false;
        }

        // POST — всі ролі
        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] SaleCreateDto dto)
        {
            if (dto == null) return BadRequest("Empty");
            if (dto.Item == null || string.IsNullOrWhiteSpace(dto.Item.Name)) return BadRequest("Item required");
            if (dto.Item.Qty <= 0) dto.Item.Qty = 1;
            if (dto.Item.Price < 0) dto.Item.Price = 0;

            var cr = await ClientResolver.ResolveOrCreateAsync(_db, dto.ClientId, dto.ClientName, dto.ClientPhone);
            if (!cr.Success) return BadRequest(cr.ErrorMessage);

            var header = new SaleHeader
            {
                ClientId  = cr.ClientId,
                ServiceId = 0,
                Price     = 0,
                Date      = DateTimeHelper.NormalizeOrNow(dto.Date),
                Payment   = dto.Payment ?? "",
                Status    = dto.Status ?? "done",
                Note      = dto.Note,
                Total     = dto.Item.Price * dto.Item.Qty
            };
            _db.SaleHeaders.Add(header);
            await _db.SaveChangesAsync();

            _db.SaleItems.Add(new SaleItem
            {
                SaleId = header.Id,
                Name   = dto.Item.Name,
                Qty    = dto.Item.Qty,
                Price  = dto.Item.Price
            });
            await _db.SaveChangesAsync();
            return Ok(new { id = header.Id });
        }

        // PUT — всі ролі
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSale(int id, [FromBody] SaleCreateDto dto)
        {
            var header = await _db.SaleHeaders.FirstOrDefaultAsync(h => h.Id == id);
            if (header == null) return NotFound();
            if (dto.Item == null || string.IsNullOrWhiteSpace(dto.Item.Name)) return BadRequest("Item required");

            var cr = await ClientResolver.ResolveOrCreateAsync(_db, dto.ClientId, dto.ClientName, dto.ClientPhone);
            if (!cr.Success) return BadRequest(cr.ErrorMessage);

            header.ClientId = cr.ClientId;
            header.Date     = DateTimeHelper.NormalizeOrNow(dto.Date);
            header.Payment  = dto.Payment ?? header.Payment;
            header.Status   = dto.Status ?? header.Status;
            header.Note     = dto.Note ?? header.Note;
            header.Total    = dto.Item.Price * dto.Item.Qty;

            var existingItem = await _db.SaleItems.FirstOrDefaultAsync(i => i.SaleId == id);
            if (existingItem != null)
            {
                existingItem.Name  = dto.Item.Name;
                existingItem.Qty   = dto.Item.Qty;
                existingItem.Price = dto.Item.Price;
            }
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE — тільки superadmin та admin (master НЕ може видаляти)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            var header = await _db.SaleHeaders.FirstOrDefaultAsync(h => h.Id == id);
            if (header == null) return NotFound();
            _db.SaleItems.RemoveRange(_db.SaleItems.Where(i => i.SaleId == id));
            _db.SaleHeaders.Remove(header);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
