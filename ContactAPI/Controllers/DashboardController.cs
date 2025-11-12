using System;
using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db) => _db = db;

        // межі "сьогодні" в UTC (бо SaleHeader.Date і Repairs.CreatedAt ми пишемо як UtcNow)
        private static (DateTime fromUtc, DateTime toUtc) TodayUtc()
        {
            var nowUtc = DateTime.UtcNow;
            var from = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
            return (from, from.AddDays(1));
        }

        // останні 7 днів (UTC)
        private static (DateTime fromUtc, DateTime toUtc) Last7DaysUtc()
        {
            var (fromToday, toToday) = TodayUtc();
            return (fromToday.AddDays(-6), toToday);
        }

        // Підрахунок продажів за діапазон:
        //  - кількість чеків
        //  - сума: Total з headers + сума по items для тих, де Total == 0
        private async Task<(int count, decimal sum)> SalesAggregate(DateTime fromUtc, DateTime toUtc)
        {
            var headersRange = _db.SaleHeaders.AsNoTracking()
                .Where(h => h.Date >= fromUtc && h.Date < toUtc);

            var count = await headersRange.CountAsync();

            var sumFromHeaders = await headersRange
                .Where(h => h.Total > 0)
                .Select(h => (decimal?)h.Total)
                .SumAsync() ?? 0m;

            var noTotalIds = await headersRange
                .Where(h => h.Total == 0)
                .Select(h => h.Id)
                .ToListAsync();

            decimal sumFromItems = 0m;
            if (noTotalIds.Count > 0)
            {
                sumFromItems = await _db.SaleItems.AsNoTracking()
                    .Where(i => noTotalIds.Contains(i.SaleId))
                    .Select(i => (decimal?)(i.Price * i.Qty))
                    .SumAsync() ?? 0m;
            }

            return (count, sumFromHeaders + sumFromItems);
        }

        // Підрахунок ремонтів за діапазон (кількість + сума TotalCost)
        private async Task<(int count, decimal sum)> RepairsAggregate(DateTime fromUtc, DateTime toUtc)
        {
            var q = _db.Repairs.AsNoTracking()
                .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt < toUtc);

            var count = await q.CountAsync();
            var sum = await q.Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m;
            return (count, sum);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var (startToday, endToday) = TodayUtc();
            var (start7, end7) = Last7DaysUtc();

            // ПРОДАЖІ
            var (salesTodayCount, salesTodaySum) = await SalesAggregate(startToday, endToday);
            var (_, weekSalesSum) = await SalesAggregate(start7, end7);

            // РЕМОНТИ
            var (repairsTodayCount, repairsTodaySum) = await RepairsAggregate(startToday, endToday);
            var (_, weekRepairsSum) = await RepairsAggregate(start7, end7);

            // КЛІЄНТИ
            var clientsTotal = await _db.Clients.AsNoTracking().CountAsync();
            var newClientsToday = await _db.Clients.AsNoTracking()
                .Where(c => c.CreatedAt >= startToday && c.CreatedAt < endToday)
                .CountAsync();

            // ОСТАННІ ПРОДАЖІ (8 шт)
            var recentSales = await (
                from h in _db.SaleHeaders.AsNoTracking()
                join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc
                from c in gc.DefaultIfEmpty()
                orderby h.Date descending
                select new
                {
                    ClientName = c != null ? c.FullName : "",
                    ItemName = _db.SaleItems.Where(si => si.SaleId == h.Id)
                                            .Select(si => si.Name)
                                            .FirstOrDefault(),
                    Price = h.Total > 0
                        ? h.Total
                        : (_db.SaleItems.Where(si => si.SaleId == h.Id)
                                        .Select(si => (decimal?)(si.Price * si.Qty))
                                        .Sum() ?? 0m)
                }
            ).Take(8).ToListAsync();

            return Ok(new
            {
                // KPI
                salesToday = salesTodayCount,
                profitSales = salesTodaySum,
                incomeWeek = weekSalesSum + weekRepairsSum,
                newClients = newClientsToday,
                repairsToday = repairsTodayCount,
                profitRepair = repairsTodaySum,
                clientsTotal = clientsTotal,

                // таблиця
                recent = recentSales.Select(x => new {
                    name = x.ClientName,
                    item = x.ItemName ?? "",
                    price = x.Price
                }).ToList()
            });
        }
    }
}
