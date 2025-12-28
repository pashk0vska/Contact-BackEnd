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
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SalesController(AppDbContext db) => _db = db;

        // Normalize any incoming DateTime to UTC.
        // - Utc: keep
        // - Local: convert
        // - Unspecified: treat as local (what browsers usually send for date-only values)
        private static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
            };
        }

        // ======== DTO + Query модель (GET) ========
        public record SalesQuery(
            string? q, string? sort = "Date", string? dir = "desc",
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

        // ======== GET /api/Sales ========
        [HttpGet]
        public async Task<IActionResult> GetSales([FromQuery] SalesQuery rq)
        {
            var baseQuery =
                from h in _db.SaleHeaders.AsNoTracking()
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
                    x.Items.Any(it => (it.Name ?? "").ToLower().Contains(q))
                );
            }

            // rq.from/to часто приходять як date-only (YYYY-MM-DD) => Kind.Unspecified.
            // В БД дати зберігаємо в UTC, тому нормалізуємо межі до UTC.
            if (rq.from.HasValue) baseQuery = baseQuery.Where(x => x.Header.Date >= NormalizeToUtc(rq.from.Value));
            if (rq.to.HasValue) baseQuery = baseQuery.Where(x => x.Header.Date <= NormalizeToUtc(rq.to.Value));
            if (!string.IsNullOrWhiteSpace(rq.status))
            {
                var s = rq.status.Trim().ToLower();
                baseQuery = baseQuery.Where(x => (x.Header.Status ?? "").ToLower() == s);
            }

            var projected = baseQuery.Select(x => new SaleListItemDto
            {
                Id = x.Header.Id,
                Date = x.Header.Date,
                ClientName = x.ClientName ?? "",
                ProductName = x.Items.Select(it => it.Name).FirstOrDefault() ?? "",
                Quantity = x.Items.Select(it => (int?)it.Qty).Sum() ?? 1,
                TotalPrice = x.Header.Total != 0
                    ? x.Header.Total
                    : (x.Items.Select(it => (decimal?)it.Price * it.Qty).Sum() ?? 0m),
                Payment = x.Header.Payment ?? "",
                Status = x.Header.Status ?? ""
            });

            bool desc = string.Equals(rq.dir, "desc", StringComparison.OrdinalIgnoreCase);
            projected = (rq.sort ?? "Date").ToLower() switch
            {
                "clientname" => desc ? projected.OrderByDescending(x => x.ClientName) : projected.OrderBy(x => x.ClientName),
                "productname" => desc ? projected.OrderByDescending(x => x.ProductName) : projected.OrderBy(x => x.ProductName),
                "quantity" => desc ? projected.OrderByDescending(x => x.Quantity) : projected.OrderBy(x => x.Quantity),
                "totalprice" => desc ? projected.OrderByDescending(x => x.TotalPrice) : projected.OrderBy(x => x.TotalPrice),
                "payment" => desc ? projected.OrderByDescending(x => x.Payment) : projected.OrderBy(x => x.Payment),
                "status" => desc ? projected.OrderByDescending(x => x.Status) : projected.OrderBy(x => x.Status),
                _ => desc ? projected.OrderByDescending(x => x.Date) : projected.OrderBy(x => x.Date),
            };

            var total = await projected.CountAsync();
            var items = await projected.Skip((rq.page - 1) * rq.pageSize).Take(rq.pageSize).ToListAsync();
            return Ok(new { items, total, page = rq.page, pageSize = rq.pageSize });
        }

        // ======== DTO для створення (POST) ========
        public class SaleCreateItemDto
        {
            public string Name { get; set; } = "";
            public int Qty { get; set; }
            public decimal Price { get; set; }
        }

        public class SaleCreateDto
        {
            public int? ClientId { get; set; }            // якщо є — використовуємо його
            public string? ClientName { get; set; }       // якщо ClientId немає — шукаємо/створюємо по імені
            public DateTime Date { get; set; }
            public string Payment { get; set; } = "";
            public string Status { get; set; } = "done";
            public string? Note { get; set; }
            public SaleCreateItemDto Item { get; set; } = new();
            public bool UpsertService { get; set; } = false; // якщо true — авто-додати в services
        }

        // GET: /api/Sales/recent?take=8
        [HttpGet("recent")]
        public async Task<IActionResult> Recent([FromQuery] int take = 8)
        {
            take = Math.Clamp(take, 1, 50);

            var q =
                from h in _db.SaleHeaders.AsNoTracking()
                join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc
                from c in gc.DefaultIfEmpty()
                join i0 in _db.SaleItems.AsNoTracking() on h.Id equals i0.SaleId into gi
                orderby h.Date descending
                select new
                {
                    name = c != null ? c.FullName : "",
                    item = gi.Select(x => x.Name).FirstOrDefault(),
                    price = h.Total
                };

            var items = await q.Take(take).ToListAsync();
            return Ok(items);
        }


        // ======== POST /api/Sales ========
        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] SaleCreateDto dto)
        {
            if (dto == null) return BadRequest("Empty payload");
            if (dto.Item == null || string.IsNullOrWhiteSpace(dto.Item.Name)) return BadRequest("Item is required");
            if (dto.Item.Qty <= 0) dto.Item.Qty = 1;
            if (dto.Item.Price < 0) dto.Item.Price = 0;

            // 1) Визначаємо клієнта
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

            // 2) (необовʼязково) додати сервіс, якщо його ще немає
            if (dto.UpsertService)
            {
                var svcName = dto.Item.Name.Trim();
                if (!string.IsNullOrWhiteSpace(svcName))
                {
                    var exists = await _db.Services.AsNoTracking()
                        .AnyAsync(s => s.Name.ToLower() == svcName.ToLower());
                    if (!exists)
                    {
                        // мінімальне створення сервісу з ціною
                        var svc = new Service
                        {
                            Name = svcName,
                            Price = dto.Item.Price
                        };
                        _db.Services.Add(svc);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            // 3) Створюємо продаж (хедер + один айтем)
            var total = dto.Item.Price * dto.Item.Qty;

            var header = new SaleHeader
            {
                ClientId = clientId,
                ServiceId = 0,
                Price = 0,
                // В БД зберігаємо UTC. Якщо з фронта прийде локальна/unspecified дата — нормалізуємо.
                Date = dto.Date == default ? DateTime.UtcNow : NormalizeToUtc(dto.Date),
                Payment = dto.Payment ?? "",
                Status = dto.Status ?? "done",
                Note = dto.Note,
                Total = total
            };

            _db.SaleHeaders.Add(header);
            await _db.SaveChangesAsync();

            var item = new SaleItem
            {
                SaleId = header.Id,
                Name = dto.Item.Name,
                Qty = dto.Item.Qty,
                Price = dto.Item.Price
            };
            _db.SaleItems.Add(item);
            await _db.SaveChangesAsync();

            return Ok(new { id = header.Id });
        }

        // ==== DELETE /api/Sales/{id} ====
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            var header = await _db.SaleHeaders.FirstOrDefaultAsync(h => h.Id == id);
            if (header == null) return NotFound();

            var items = _db.SaleItems.Where(i => i.SaleId == id);
            _db.SaleItems.RemoveRange(items);
            _db.SaleHeaders.Remove(header);

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }


}
