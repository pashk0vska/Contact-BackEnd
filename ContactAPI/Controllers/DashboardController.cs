using Contact.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin,master")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext db, ILogger<DashboardController> logger)
        {
            _db = db; _logger = logger;
        }

        static (DateTime f, DateTime t) TodayUtc()
        {
            var tz = TimeZoneInfo.Local;
            var nl = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var su = TimeZoneInfo.ConvertTimeToUtc(nl.Date, tz);
            return (su, su.AddDays(1));
        }

        static (DateTime f, DateTime t) Last7()  { var (f, t) = TodayUtc(); return (f.AddDays(-6), t); }
        static (DateTime f, DateTime t) ThisMonth()
        {
            var tz = TimeZoneInfo.Local;
            var nl = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var su = TimeZoneInfo.ConvertTimeToUtc(new DateTime(nl.Year, nl.Month, 1), tz);
            return (su, su.AddMonths(1));
        }

        async Task<(int c, decimal s)> SalesAgg(DateTime f, DateTime t)
        {
            var q = _db.SaleHeaders.AsNoTracking().Where(h => h.Date >= f && h.Date < t);
            return (await q.CountAsync(), await q.Select(h => (decimal?)h.Total).SumAsync() ?? 0m);
        }

        async Task<(int c, decimal s)> RepAgg(DateTime f, DateTime t)
        {
            var q = _db.Repairs.AsNoTracking().Where(r => r.CreatedAt >= f && r.CreatedAt < t);
            return (await q.CountAsync(), await q.Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var (st, et) = TodayUtc();
            var (s7, e7) = Last7();
            var (sm, em) = ThisMonth();

            var (stc, sts) = await SalesAgg(st, et);
            var (_, ws)    = await SalesAgg(s7, e7);
            var (smc, sms) = await SalesAgg(sm, em);
            var (rtc, rts) = await RepAgg(st, et);
            var (_2, wr)   = await RepAgg(s7, e7);
            var (rmc, rms) = await RepAgg(sm, em);
            var ct = await _db.Clients.AsNoTracking().CountAsync();
            var nc = await _db.Clients.AsNoTracking().Where(c => c.CreatedAt >= st && c.CreatedAt < et).CountAsync();

            var recent = await (from h in _db.SaleHeaders.AsNoTracking()
                               join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc
                               from c in gc.DefaultIfEmpty()
                               orderby h.Date descending
                               select new
                               {
                                   name  = c != null ? c.FullName : "",
                                   item  = _db.SaleItems.Where(si => si.SaleId == h.Id).Select(si => si.Name).FirstOrDefault(),
                                   price = h.Total
                               }).Take(8).ToListAsync();

            return Ok(new
            {
                salesToday  = stc, profitSales = sts,
                repairsToday = rtc, profitRepair = rts,
                newClients  = nc,
                incomeWeek  = ws + wr,
                salesMonth  = smc, salesMonthSum = sms,
                repairsMonth = rmc, repairsMonthSum = rms,
                totalIncomeMonth = sms + rms,
                clientsTotal = ct,
                repairsByStatus = await _db.Repairs.AsNoTracking()
                    .GroupBy(r => r.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToListAsync(),
                recent = recent.Select(x => new { x.name, item = x.item ?? "", x.price }).ToList()
            });
        }
    }
}
