<<<<<<< HEAD
using Contact.API.Data; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
=======
﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

>>>>>>> f98bf5a (chore: cleanup gitignore, remove build artifacts)
namespace Contact.API.Controllers
{
    [ApiController][Route("api/[controller]")][Authorize]
    public class DashboardController : ControllerBase
    {
<<<<<<< HEAD
        private readonly AppDbContext _db; private readonly ILogger<DashboardController> _logger;
        public DashboardController(AppDbContext db, ILogger<DashboardController> logger){_db=db;_logger=logger;}
        static (DateTime f,DateTime t) TodayUtc(){var tz=TimeZoneInfo.Local;var nl=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,tz);var su=TimeZoneInfo.ConvertTimeToUtc(nl.Date,tz);return(su,su.AddDays(1));}
        static (DateTime f,DateTime t) Last7(){var(f,t)=TodayUtc();return(f.AddDays(-6),t);}
        static (DateTime f,DateTime t) ThisMonth(){var tz=TimeZoneInfo.Local;var nl=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,tz);var su=TimeZoneInfo.ConvertTimeToUtc(new DateTime(nl.Year,nl.Month,1),tz);return(su,su.AddMonths(1));}
        async Task<(int c,decimal s)> SalesAgg(DateTime f,DateTime t){var q=_db.SaleHeaders.AsNoTracking().Where(h=>h.Date>=f&&h.Date<t);return(await q.CountAsync(),await q.Select(h=>(decimal?)h.Total).SumAsync()??0m);}
        async Task<(int c,decimal s)> RepAgg(DateTime f,DateTime t){var q=_db.Repairs.AsNoTracking().Where(r=>r.CreatedAt>=f&&r.CreatedAt<t);return(await q.CountAsync(),await q.Select(r=>(decimal?)r.TotalCost).SumAsync()??0m);}
=======
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

        // Повний дашборд — тільки superadmin та admin
>>>>>>> f98bf5a (chore: cleanup gitignore, remove build artifacts)
        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var(st,et)=TodayUtc();var(s7,e7)=Last7();var(sm,em)=ThisMonth();
            var(stc,sts)=await SalesAgg(st,et);var(_,ws)=await SalesAgg(s7,e7);var(smc,sms)=await SalesAgg(sm,em);
            var(rtc,rts)=await RepAgg(st,et);var(_2,wr)=await RepAgg(s7,e7);var(rmc,rms)=await RepAgg(sm,em);
            var ct=await _db.Clients.AsNoTracking().CountAsync();var nc=await _db.Clients.AsNoTracking().Where(c=>c.CreatedAt>=st&&c.CreatedAt<et).CountAsync();
            var recent=await(from h in _db.SaleHeaders.AsNoTracking() join c0 in _db.Clients.AsNoTracking() on h.ClientId equals c0.Id into gc from c in gc.DefaultIfEmpty() orderby h.Date descending select new{name=c!=null?c.FullName:"",item=_db.SaleItems.Where(si=>si.SaleId==h.Id).Select(si=>si.Name).FirstOrDefault(),price=h.Total}).Take(8).ToListAsync();
            return Ok(new{salesToday=stc,profitSales=sts,repairsToday=rtc,profitRepair=rts,newClients=nc,incomeWeek=ws+wr,salesMonth=smc,salesMonthSum=sms,repairsMonth=rmc,repairsMonthSum=rms,totalIncomeMonth=sms+rms,clientsTotal=ct,repairsByStatus=await _db.Repairs.AsNoTracking().GroupBy(r=>r.Status).Select(g=>new{status=g.Key,count=g.Count()}).ToListAsync(),recent=recent.Select(x=>new{x.name,item=x.item??"",x.price}).ToList()});
        }

        // Спрощений дашборд для master — тільки кількості без фінансів
        [HttpGet("summary/master")]
        [Authorize(Roles = "superadmin,admin,master")]
        public async Task<IActionResult> SummaryMaster()
        {
            _logger.LogInformation("Отримання даних дашборду для майстра.");

            var (startToday, endToday) = TodayUtcByLocalDay();

            var salesToday = await _db.SaleHeaders.AsNoTracking()
                .Where(h => h.Date >= startToday && h.Date < endToday)
                .CountAsync();

            var repairsToday = await _db.Repairs.AsNoTracking()
                .Where(r => r.CreatedAt >= startToday && r.CreatedAt < endToday)
                .CountAsync();

            var clientsTotal = await _db.Clients.AsNoTracking().CountAsync();

            var repairsByStatus = await _db.Repairs.AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

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
                                            .FirstOrDefault()
                }
            ).Take(8).ToListAsync();

            return Ok(new
            {
                salesToday,
                repairsToday,
                clientsTotal,
                repairsByStatus,
                recent = recentSales.Select(x => new
                {
                    name = x.ClientName,
                    item = x.ItemName ?? ""
                }).ToList()
            });
        }
    }
}
