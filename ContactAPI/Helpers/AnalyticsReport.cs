using Microsoft.EntityFrameworkCore;
using Contact.API.Data;

namespace Contact.API.Helpers
{
    // ===== DTO-моделі звіту =====
    public class Metric
    {
        public string Label = "";
        public decimal Current;
        public decimal? Previous;       // значення за попередній рівний період
        public bool IsMoney;
        public bool IsPercent;
        public string? Suffix;
        public bool MoreIsBetter = true;
        public decimal? DeltaPct => (Previous.HasValue && Previous.Value != 0m)
            ? decimal.Round((Current - Previous.Value) / Math.Abs(Previous.Value) * 100m, 1)
            : (Previous.HasValue && Previous.Value == 0m && Current != 0m ? (decimal?)null : null);
    }

    public class NamedValue { public string Name = ""; public decimal Value; public decimal Share; }
    public class ProductRow { public string Product = ""; public string Category = ""; public int Qty; public decimal Sum; public decimal AvgPrice; public decimal Share; }
    public class ServiceRow { public string Name = ""; public int Count; public decimal Sum; }
    public class StatusRow { public string Status = ""; public int Count; public decimal Share; }
    public class DeviceRow { public string Device = ""; public int Count; public decimal Sum; public decimal Avg; }
    public class MasterRow { public string Name = ""; public int SalesCount; public decimal SalesSum; public int RepairsCount; public decimal RepairsSum; public decimal Total => SalesSum + RepairsSum; }
    public class PaymentRow { public string Method = ""; public int Count; public decimal Sum; public decimal Share; }
    public class DayRow { public DateTime Date; public decimal Sales; public decimal Repairs; public decimal Total => Sales + Repairs; }
    public class WeekdayRow { public string Name = ""; public decimal Value; }

    public class ReportData
    {
        public DateTime FromLocal;
        public DateTime ToLocal;          // включна дата (остання доба періоду)
        public string Mode = "all";       // all | sales | repairs
        public bool IncludeSales;
        public bool IncludeRepairs;
        public int Days;
        public DateTime GeneratedAt;

        // KPI
        public decimal Income, SalesRevenue, RepairsRevenue;
        public int SalesCount, RepairsCount, NewClients;
        public decimal AvgCheck, AvgRepairCost, ItemsPerSale, CompletionRate;

        public List<Metric> Kpis = new();

        // Розрізи
        public List<NamedValue> ByCategory = new();
        public List<ProductRow> TopProducts = new();
        public List<ServiceRow> TopServices = new();
        public List<StatusRow> RepairsByStatus = new();
        public List<DeviceRow> RepairsByDevice = new();
        public List<MasterRow> StaffPerformance = new();
        public List<PaymentRow> Payments = new();
        public List<DayRow> Daily = new();
        public List<WeekdayRow> Weekdays = new();

        // Підсумкова статистика чеків
        public decimal MinCheck, MaxCheck, MedianCheck;
        public int TotalItems;
        public DayRow? BestDay;

        public string PeriodText => $"{FromLocal:dd.MM.yyyy} — {ToLocal:dd.MM.yyyy}";
        public string ModeText => Mode switch { "sales" => "Лише продажі", "repairs" => "Лише ремонти", _ => "Продажі + ремонти" };
    }

    public static class AnalyticsReport
    {
        private static readonly string[] WdUa = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд" };

        public static string RepStatusUa(string? s) => (s ?? "").Trim().ToLowerInvariant() switch
        {
            "new" or "новий" => "Новий",
            "progress" or "in_progress" or "inprogress" or "в процесі" => "В процесі",
            "done" or "ready" or "готово" => "Готово",
            "issued" or "видано" => "Видано",
            "canceled" or "cancelled" or "скасовано" => "Скасовано",
            _ => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim()
        };

        public static string PaymentUa(string? p) => (p ?? "").Trim().ToLowerInvariant() switch
        {
            "cash" or "готівка" => "Готівка",
            "card" or "картка" or "карта" => "Картка",
            "transfer" or "переказ" => "Переказ",
            "mixed" or "змішано" => "Змішано",
            "" => "Не вказано",
            _ => p!.Trim()
        };

        public static async Task<ReportData> BuildAsync(AppDbContext db, DateTime? from, DateTime? to, string? type)
        {
            var tz = TimeZoneInfo.Local;
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startLocal = from?.Date ?? nowLocal.Date.AddDays(-29);
            var endLELocal = (to?.Date ?? nowLocal.Date).AddDays(1);           // ексклюзивна верхня межа
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLELocal, tz);

            var days = Math.Max(1, (int)(endLELocal - startLocal).TotalDays);

            // Попередній рівний період (для порівняння динаміки)
            var prevStartLocal = startLocal.AddDays(-days);
            var prevEndLELocal = startLocal;
            var prevStartUtc = TimeZoneInfo.ConvertTimeToUtc(prevStartLocal, tz);
            var prevEndUtc = TimeZoneInfo.ConvertTimeToUtc(prevEndLELocal, tz);

            var mode = (type ?? "all").Trim().ToLowerInvariant();
            var incS = mode == "all" || mode == "sales";
            var incR = mode == "all" || mode == "repairs";

            var d = new ReportData
            {
                FromLocal = startLocal,
                ToLocal = endLELocal.AddDays(-1),
                Mode = mode,
                IncludeSales = incS,
                IncludeRepairs = incR,
                Days = days,
                GeneratedAt = nowLocal
            };

            // ===== Витягуємо «сирі» дані у пам'ять (невеликі обсяги) =====
            var sales = incS
                ? await db.SaleHeaders.AsNoTracking()
                    .Where(h => h.Date >= startUtc && h.Date < endUtc)
                    .Select(h => new { h.Id, h.Date, h.Total, h.Payment, h.MasterId })
                    .ToListAsync()
                : new();

            var items = incS
                ? await (from si in db.SaleItems.AsNoTracking()
                         join h in db.SaleHeaders.AsNoTracking() on si.SaleId equals h.Id
                         where h.Date >= startUtc && h.Date < endUtc
                         select new { si.Name, si.Type, si.Qty, si.Price }).ToListAsync()
                : new();

            var repairs = incR
                ? await db.Repairs.AsNoTracking()
                    .Where(r => r.CreatedAt >= startUtc && r.CreatedAt < endUtc)
                    .Select(r => new { r.Status, r.DeviceType, r.TotalCost, r.MasterId, r.CreatedAt })
                    .ToListAsync()
                : new();

            var masters = (await db.Users.AsNoTracking()
                    .Where(u => u.Role == "master")
                    .Select(u => new { u.Id, u.Username }).ToListAsync())
                .ToDictionary(x => x.Id, x => x.Username);

            d.NewClients = await db.Clients.AsNoTracking()
                .Where(c => c.CreatedAt >= startUtc && c.CreatedAt < endUtc).CountAsync();

            // ===== KPI =====
            d.SalesCount = sales.Count;
            d.SalesRevenue = sales.Sum(s => s.Total);
            d.RepairsCount = repairs.Count;
            d.RepairsRevenue = repairs.Sum(r => r.TotalCost);
            d.Income = d.SalesRevenue + d.RepairsRevenue;
            d.AvgCheck = d.SalesCount > 0 ? decimal.Round(d.SalesRevenue / d.SalesCount, 2) : 0m;
            d.AvgRepairCost = d.RepairsCount > 0 ? decimal.Round(d.RepairsRevenue / d.RepairsCount, 2) : 0m;
            d.TotalItems = items.Sum(i => i.Qty);
            d.ItemsPerSale = d.SalesCount > 0 ? decimal.Round((decimal)d.TotalItems / d.SalesCount, 2) : 0m;

            // ===== Попередній період (агрегати) =====
            decimal prevSalesRev = 0m, prevRepRev = 0m;
            int prevSalesCount = 0, prevRepCount = 0, prevNewCl = 0;
            if (incS)
            {
                prevSalesCount = await db.SaleHeaders.AsNoTracking().Where(h => h.Date >= prevStartUtc && h.Date < prevEndUtc).CountAsync();
                prevSalesRev = await db.SaleHeaders.AsNoTracking().Where(h => h.Date >= prevStartUtc && h.Date < prevEndUtc).Select(h => (decimal?)h.Total).SumAsync() ?? 0m;
            }
            if (incR)
            {
                prevRepCount = await db.Repairs.AsNoTracking().Where(r => r.CreatedAt >= prevStartUtc && r.CreatedAt < prevEndUtc).CountAsync();
                prevRepRev = await db.Repairs.AsNoTracking().Where(r => r.CreatedAt >= prevStartUtc && r.CreatedAt < prevEndUtc).Select(r => (decimal?)r.TotalCost).SumAsync() ?? 0m;
            }
            prevNewCl = await db.Clients.AsNoTracking().Where(c => c.CreatedAt >= prevStartUtc && c.CreatedAt < prevEndUtc).CountAsync();
            var prevIncome = prevSalesRev + prevRepRev;
            var prevAvg = prevSalesCount > 0 ? decimal.Round(prevSalesRev / prevSalesCount, 2) : 0m;

            d.Kpis.Add(new Metric { Label = "Загальний дохід", Current = d.Income, Previous = prevIncome, IsMoney = true });
            if (incS) d.Kpis.Add(new Metric { Label = "Кількість продажів", Current = d.SalesCount, Previous = prevSalesCount });
            if (incR) d.Kpis.Add(new Metric { Label = "Кількість ремонтів", Current = d.RepairsCount, Previous = prevRepCount });
            if (incS) d.Kpis.Add(new Metric { Label = "Середній чек", Current = d.AvgCheck, Previous = prevAvg, IsMoney = true });
            if (incR) d.Kpis.Add(new Metric { Label = "Сер. вартість ремонту", Current = d.AvgRepairCost, IsMoney = true });
            d.Kpis.Add(new Metric { Label = "Нових клієнтів", Current = d.NewClients, Previous = prevNewCl });
            if (incS) d.Kpis.Add(new Metric { Label = "Позицій у продажах", Current = d.TotalItems });
            if (incS) d.Kpis.Add(new Metric { Label = "Позицій на чек", Current = d.ItemsPerSale });

            // ===== Дохід за категоріями =====
            decimal catProducts = 0m, catServices = 0m, catBuilds = 0m;
            foreach (var r in items)
            {
                var amt = r.Price * r.Qty;
                switch ((r.Type ?? "product").ToLowerInvariant())
                {
                    case "service": catServices += amt; break;
                    case "build": catBuilds += amt; break;
                    default: catProducts += amt; break;
                }
            }
            var cats = new List<NamedValue>
            {
                new() { Name = "Ремонти", Value = incR ? d.RepairsRevenue : 0m },
                new() { Name = "Товари",  Value = catProducts },
                new() { Name = "Збірки",  Value = catBuilds },
                new() { Name = "Послуги", Value = catServices },
            };
            var catTotal = cats.Sum(c => c.Value);
            foreach (var c in cats) c.Share = catTotal > 0 ? decimal.Round(c.Value / catTotal * 100m, 1) : 0m;
            d.ByCategory = cats.Where(c => c.Value > 0 || (c.Name == "Ремонти" && incR) || (c.Name != "Ремонти" && incS)).ToList();

            // ===== ТОП товарів/послуг =====
            if (incS)
            {
                var grouped = items.GroupBy(x => x.Name)
                    .Select(g => new ProductRow
                    {
                        Product = g.Key,
                        Category = MapCat(g.First().Type),
                        Qty = g.Sum(x => x.Qty),
                        Sum = g.Sum(x => x.Price * x.Qty)
                    }).ToList();
                var prodTotal = grouped.Sum(x => x.Sum);
                foreach (var p in grouped)
                {
                    p.AvgPrice = p.Qty > 0 ? decimal.Round(p.Sum / p.Qty, 2) : 0m;
                    p.Share = prodTotal > 0 ? decimal.Round(p.Sum / prodTotal * 100m, 1) : 0m;
                }
                d.TopProducts = grouped.OrderByDescending(x => x.Sum).Take(10).ToList();

                d.TopServices = items.Where(x => (x.Type ?? "").ToLowerInvariant() == "service")
                    .GroupBy(x => x.Name)
                    .Select(g => new ServiceRow { Name = g.Key, Count = g.Sum(x => x.Qty), Sum = g.Sum(x => x.Price * x.Qty) })
                    .OrderByDescending(x => x.Count).Take(8).ToList();

                // ===== Способи оплати =====
                var pays = sales.GroupBy(s => PaymentUa(s.Payment))
                    .Select(g => new PaymentRow { Method = g.Key, Count = g.Count(), Sum = g.Sum(x => x.Total) })
                    .OrderByDescending(x => x.Sum).ToList();
                var paySum = pays.Sum(x => x.Sum);
                foreach (var p in pays) p.Share = paySum > 0 ? decimal.Round(p.Sum / paySum * 100m, 1) : 0m;
                d.Payments = pays;

                // ===== Статистика чеків =====
                if (sales.Count > 0)
                {
                    var totals = sales.Select(s => s.Total).OrderBy(x => x).ToList();
                    d.MinCheck = totals.First();
                    d.MaxCheck = totals.Last();
                    int n = totals.Count;
                    d.MedianCheck = n % 2 == 1 ? totals[n / 2] : decimal.Round((totals[n / 2 - 1] + totals[n / 2]) / 2m, 2);
                }
            }

            // ===== Ремонти: статуси / пристрої =====
            if (incR)
            {
                var byStatus = repairs.GroupBy(r => RepStatusUa(r.Status))
                    .Select(g => new StatusRow { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();
                foreach (var s in byStatus) s.Share = d.RepairsCount > 0 ? decimal.Round((decimal)s.Count / d.RepairsCount * 100m, 1) : 0m;
                d.RepairsByStatus = byStatus;

                d.RepairsByDevice = repairs.GroupBy(r => string.IsNullOrWhiteSpace(r.DeviceType) ? "—" : r.DeviceType)
                    .Select(g => new DeviceRow { Device = g.Key, Count = g.Count(), Sum = g.Sum(x => x.TotalCost), Avg = decimal.Round(g.Average(x => x.TotalCost), 2) })
                    .OrderByDescending(x => x.Sum).Take(8).ToList();

                var doneCount = repairs.Count(r => { var s = RepStatusUa(r.Status); return s == "Готово" || s == "Видано"; });
                d.CompletionRate = d.RepairsCount > 0 ? decimal.Round((decimal)doneCount / d.RepairsCount * 100m, 1) : 0m;
            }

            // ===== Ефективність персоналу =====
            var staff = new Dictionary<string, MasterRow>();
            MasterRow GetM(int? id)
            {
                var name = id.HasValue && masters.TryGetValue(id.Value, out var u) ? u : "Не призначено";
                if (!staff.TryGetValue(name, out var row)) { row = new MasterRow { Name = name }; staff[name] = row; }
                return row;
            }
            if (incS) foreach (var s in sales) { var m = GetM(s.MasterId); m.SalesCount++; m.SalesSum += s.Total; }
            if (incR) foreach (var r in repairs) { var m = GetM(r.MasterId); m.RepairsCount++; m.RepairsSum += r.TotalCost; }
            d.StaffPerformance = staff.Values.OrderByDescending(x => x.Total).ToList();

            // ===== Динаміка по днях + найкращий день + дні тижня =====
            var byDay = new Dictionary<DateTime, DayRow>();
            for (var dt = startLocal; dt < endLELocal; dt = dt.AddDays(1)) byDay[dt] = new DayRow { Date = dt };
            DateTime LocalDate(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz).Date;
            if (incS) foreach (var s in sales) { var k = LocalDate(s.Date); if (byDay.TryGetValue(k, out var row)) row.Sales += s.Total; }
            if (incR) foreach (var r in repairs) { var k = LocalDate(r.CreatedAt); if (byDay.TryGetValue(k, out var row)) row.Repairs += r.TotalCost; }
            d.Daily = byDay.Values.OrderBy(x => x.Date).ToList();
            d.BestDay = d.Daily.Count > 0 ? d.Daily.OrderByDescending(x => x.Total).First() : null;
            if (d.BestDay != null && d.BestDay.Total == 0m) d.BestDay = null;

            var wd = new decimal[7];
            foreach (var row in d.Daily) { int idx = ((int)row.Date.DayOfWeek + 6) % 7; wd[idx] += row.Total; }
            d.Weekdays = Enumerable.Range(0, 7).Select(i => new WeekdayRow { Name = WdUa[i], Value = wd[i] }).ToList();

            return d;
        }

        private static string MapCat(string? type) => (type ?? "product").ToLowerInvariant() switch
        {
            "service" => "Послуга",
            "build" => "Збірка",
            _ => "Товар"
        };
    }
}
