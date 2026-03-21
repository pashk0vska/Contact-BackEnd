using System;
using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext db, ILogger<DashboardController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private static (DateTime fromUtc, DateTime toUtc) TodayUtcByLocalDay()
        {
            var tz = TimeZoneInfo.Local;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startLocal = nowLocal.Date;
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            return (startUtc, startUtc.AddDays(1));
        }

        private static (DateTime fromUtc, DateTime toUtc) Last7DaysUtcByLocalDay()
        {
            var (fromTodayUtc, toTodayUtc) = TodayUtcByLocalDay();
            return (fromTodayUtc.AddDays(-6), toTodayUtc);
        }

        private static (DateTime fromUtc, DateTime toUtc) ThisMonthUtcByLocalDay()
        {
            var tz = TimeZoneInfo.Local;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            return (startUtc, startUtc.AddMonths(1));
        }

        private async Task<(int count, decimal sum)> SalesAggregate(DateTime from, DateTime to)
        {
            var headersRange = _db.SaleHeaders.AsNoTracking()
                .Where(h => h.Date >= from && h.Date < to);

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

        private async Task<(int count, decimal sum)> RepairsAggregate(DateTime from, DateTime to)
        {
            var q = _db.Repairs.AsNoTracking()
                .Where(r => r.CreatedAt >= from && r.CreatedAt < to);

            var count = await q.CountAsync();
            var sum = await q.Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m;
            return (count, sum);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            _logger.LogInformation("Отримання даних дашборду.");

            var (startToday, endToday) = TodayUtcByLocalDay();
            var (start7, end7) = Last7DaysUtcByLocalDay();
            var (startMonth, endMonth) = ThisMonthUtcByLocalDay();

            // ПРОДАЖІ
            var (salesTodayCount, salesTodaySum) = await SalesAggregate(startToday, endToday);
            var (_, weekSalesSum) = await SalesAggregate(start7, end7);
            var (salesMonthCount, salesMonthSum) = await SalesAggregate(startMonth, endMonth);

            // РЕМОНТИ
            var (repairsTodayCount, repairsTodaySum) = await RepairsAggregate(startToday, endToday);
            var (_, weekRepairsSum) = await RepairsAggregate(start7, end7);
            var (repairsMonthCount, repairsMonthSum) = await RepairsAggregate(startMonth, endMonth);

            // КЛІЄНТИ
            var clientsTotal = await _db.Clients.AsNoTracking().CountAsync();
            var newClientsToday = await _db.Clients.AsNoTracking()
                .Where(c => c.CreatedAt >= startToday && c.CreatedAt < endToday)
                .CountAsync();

            // РЕМОНТИ ПО СТАТУСАХ
            var repairsByStatus = await _db.Repairs.AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            // ОСТАННІ ПРОДАЖІ
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
                // KPI сьогодні
                salesToday = salesTodayCount,
                profitSales = salesTodaySum,
                repairsToday = repairsTodayCount,
                profitRepair = repairsTodaySum,
                newClients = newClientsToday,

                // KPI тиждень
                incomeWeek = weekSalesSum + weekRepairsSum,

                // KPI місяць
                salesMonth = salesMonthCount,
                salesMonthSum = salesMonthSum,
                repairsMonth = repairsMonthCount,
                repairsMonthSum = repairsMonthSum,
                totalIncomeMonth = salesMonthSum + repairsMonthSum,

                // Загальне
                clientsTotal = clientsTotal,

                // Статуси ремонтів
                repairsByStatus = repairsByStatus,

                // Таблиця останніх продажів
                recent = recentSales.Select(x => new
                {
                    name = x.ClientName,
                    item = x.ItemName ?? "",
                    price = x.Price
                }).ToList()
            });
        }
    }
}