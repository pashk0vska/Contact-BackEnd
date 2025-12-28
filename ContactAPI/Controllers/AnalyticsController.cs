using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;

namespace Contact.API.Controllers
{
    /// <summary>
    /// Аналітика за обраний період.
    /// Дати в БД зберігаються в UTC. Межі періоду трактуються як локальні календарні дні,
    /// але переводяться в UTC для запитів до БД.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AnalyticsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Повертає KPI за період та ТОП-10 товарів/послуг (по продажах).
        /// 
        /// GET /api/Analytics/summary?from=2025-12-01&to=2025-12-25&type=all
        /// type: all | sales | repairs
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type)
        {
            var tz = TimeZoneInfo.Local;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            // Якщо користувач не обрав період, беремо останні 30 днів (включно) по локальному календарю.
            var startLocal = (from?.Date ?? nowLocal.Date.AddDays(-29));
            var endLocalExclusive = (to?.Date ?? nowLocal.Date).AddDays(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocalExclusive, tz);

            var mode = (type ?? "all").Trim().ToLowerInvariant();
            var includeSales = mode == "all" || mode == "sales";
            var includeRepairs = mode == "all" || mode == "repairs";

            // SALES
            var salesQuery = _db.SaleHeaders.AsNoTracking()
                .Where(h => h.Date >= startUtc && h.Date < endUtc);

            var salesCount = includeSales ? await salesQuery.CountAsync() : 0;
            var salesRevenue = includeSales
                ? await salesQuery.Select(h => (decimal?)h.Total).SumAsync() ?? 0m
                : 0m;

            // REPAIRS
            var repairsQuery = _db.Repairs.AsNoTracking()
                .Where(r => r.CreatedAt >= startUtc && r.CreatedAt < endUtc);

            var repairsCount = includeRepairs ? await repairsQuery.CountAsync() : 0;
            var repairsRevenue = includeRepairs
                ? await repairsQuery.Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m
                : 0m;

            // CLIENTS
            var newClients = await _db.Clients.AsNoTracking()
                .Where(c => c.CreatedAt >= startUtc && c.CreatedAt < endUtc)
                .CountAsync();

            var income = salesRevenue + repairsRevenue;
            var avgCheck = salesCount > 0 ? decimal.Round(salesRevenue / salesCount, 2) : 0m;

            // TOP-10 products in selected range.
            // NOTE: Some EF Core + MySQL/MariaDB providers can't translate complex GroupBy+Sum.
            // We fetch minimal rows first, then aggregate in-memory (safe + stable for this mini-CRM).
            List<TopItemDto> topItems;
            if (includeSales)
            {
                var rows = await (
                    from si in _db.SaleItems.AsNoTracking()
                    join h in _db.SaleHeaders.AsNoTracking() on si.SaleId equals h.Id
                    where h.Date >= startUtc && h.Date < endUtc
                    select new { si.Name, si.Qty, si.Price }
                ).ToListAsync();

                topItems = rows
                    .GroupBy(x => x.Name)
                    .Select(g => new TopItemDto
                    {
                        name = g.Key,
                        qty = g.Sum(x => x.Qty),
                        sum = g.Sum(x => (decimal)x.Price * x.Qty)
                    })
                    .OrderByDescending(x => x.sum)
                    .Take(10)
                    .ToList();
            }
            else
            {
                topItems = new List<TopItemDto>();
            }

            // Try to enrich category by matching to Services.Name
            var serviceByName = await _db.Services.AsNoTracking()
                .Select(s => new { s.Name, s.Category })
                .ToListAsync();

            string GetCategory(string itemName)
            {
                var hit = serviceByName.FirstOrDefault(x => x.Name == itemName);
                return hit?.Category ?? "Продажі";
            }

            return Ok(new
            {
                period = new
                {
                    from = startLocal.ToString("yyyy-MM-dd"),
                    to = endLocalExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
                    type = mode
                },
                kpi = new
                {
                    income,
                    salesCount,
                    repairsCount,
                    avgCheck,
                    profitEstimate = income, // як і на дашборді: "прибуток" = сума за період
                    newClients
                },
                topProducts = topItems.Select(x => new
                {
                    product = x.name,
                    category = GetCategory(x.name),
                    qty = x.qty,
                    sum = x.sum
                }).ToList()
            });
        }
    }

    public class TopItemDto
    {
        public string name { get; set; } = "";
        public int qty { get; set; }
        public decimal sum { get; set; }
    }
}
