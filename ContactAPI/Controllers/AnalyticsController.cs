using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using Contact.API.Helpers;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AnalyticsController(AppDbContext db) => _db = db;

        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type)
        {
            var tz = TimeZoneInfo.Local;
            var nl = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startLocal = (from?.Date ?? nl.Date.AddDays(-29));
            var endLE      = (to?.Date ?? nl.Date).AddDays(1);
            var startUtc   = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var endUtc     = TimeZoneInfo.ConvertTimeToUtc(endLE, tz);

            var mode = (type ?? "all").Trim().ToLowerInvariant();
            var incS = mode == "all" || mode == "sales";
            var incR = mode == "all" || mode == "repairs";

            var salesQ     = _db.SaleHeaders.AsNoTracking().Where(h => h.Date >= startUtc && h.Date < endUtc);
            var salesCount = incS ? await salesQ.CountAsync() : 0;
            var salesRev   = incS ? await salesQ.Select(h => (decimal?)h.Total).SumAsync() ?? 0m : 0m;

            var repQ     = _db.Repairs.AsNoTracking().Where(r => r.CreatedAt >= startUtc && r.CreatedAt < endUtc);
            var repCount = incR ? await repQ.CountAsync() : 0;
            var repRev   = incR ? await repQ.Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m : 0m;

            var newCl    = await _db.Clients.AsNoTracking().Where(c => c.CreatedAt >= startUtc && c.CreatedAt < endUtc).CountAsync();
            var income   = salesRev + repRev;
            var avgCheck = salesCount > 0 ? decimal.Round(salesRev / salesCount, 2) : 0m;

            // Топ товарів + дохід за категоріями + топ послуг
            List<object> topItems = new();
            List<object> topServices = new();
            decimal catProducts = 0m, catServices = 0m, catBuilds = 0m;
            if (incS)
            {
                var rows = await (from si in _db.SaleItems.AsNoTracking()
                                  join h in _db.SaleHeaders.AsNoTracking() on si.SaleId equals h.Id
                                  where h.Date >= startUtc && h.Date < endUtc
                                  select new { si.Name, si.Type, si.Qty, si.Price }).ToListAsync();

                topItems = rows.GroupBy(x => x.Name)
                    .Select(g => new { product = g.Key, category = "Продажі", qty = g.Sum(x => x.Qty), sum = g.Sum(x => (decimal)x.Price * x.Qty) })
                    .OrderByDescending(x => x.sum)
                    .Take(10)
                    .Cast<object>()
                    .ToList();

                foreach (var r in rows)
                {
                    var amt = (decimal)r.Price * r.Qty;
                    switch ((r.Type ?? "product").ToLower())
                    {
                        case "service": catServices += amt; break;
                        case "build":   catBuilds   += amt; break;
                        default:        catProducts += amt; break;
                    }
                }

                topServices = rows.Where(x => (x.Type ?? "").ToLower() == "service")
                    .GroupBy(x => x.Name)
                    .Select(g => new { name = g.Key, count = g.Sum(x => x.Qty) })
                    .OrderByDescending(x => x.count)
                    .Take(8)
                    .Cast<object>()
                    .ToList();
            }

            // Ремонти: розподіл за статусами + топ типів пристроїв (для режиму «Лише ремонти»)
            List<object> repairsByStatus = new();
            List<object> repairsByDevice = new();
            if (incR)
            {
                repairsByStatus = (await repQ
                    .GroupBy(r => r.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToListAsync())
                    .Cast<object>().ToList();

                repairsByDevice = (await repQ
                    .GroupBy(r => r.DeviceType)
                    .Select(g => new { device = g.Key, count = g.Count(), sum = g.Sum(x => x.TotalCost) })
                    .OrderByDescending(x => x.sum)
                    .Take(8)
                    .ToListAsync())
                    .Cast<object>().ToList();
            }

            var byCategory = new[]
            {
                new { name = "Ремонти", value = incR ? repRev : 0m },
                new { name = "Товари",  value = catProducts },
                new { name = "Збірки",  value = catBuilds },
                new { name = "Послуги", value = catServices }
            };

            return Ok(new
            {
                period = new { from = startLocal.ToString("yyyy-MM-dd"), to = endLE.AddDays(-1).ToString("yyyy-MM-dd"), type = mode },
                kpi    = new { income, salesCount, repairsCount = repCount, avgCheck, profitEstimate = income, newClients = newCl },
                topProducts = topItems,
                byCategory,
                topServices,
                repairsByStatus,
                repairsByDevice
            });
        }

        [HttpGet("report-pdf")]
        public async Task<IActionResult> ReportPdf([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type)
        {
            var data = await AnalyticsReport.BuildAsync(_db, from, to, type);
            var bytes = AnalyticsPdf.Build(data);
            var name = $"kontakt-zvit_{data.FromLocal:yyyy-MM-dd}_{data.ToLocal:yyyy-MM-dd}.pdf";
            return File(bytes, "application/pdf", name);
        }

        [HttpGet("report-excel")]
        public async Task<IActionResult> ReportExcel([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type)
        {
            var data = await AnalyticsReport.BuildAsync(_db, from, to, type);
            var bytes = AnalyticsExcel.Build(data);
            var name = $"kontakt-zvit_{data.FromLocal:yyyy-MM-dd}_{data.ToLocal:yyyy-MM-dd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
        }
    }
}
