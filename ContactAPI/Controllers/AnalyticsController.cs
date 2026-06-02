using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

            List<object> topItems = new();
            if (incS)
            {
                var rows = await (from si in _db.SaleItems.AsNoTracking()
                                  join h in _db.SaleHeaders.AsNoTracking() on si.SaleId equals h.Id
                                  where h.Date >= startUtc && h.Date < endUtc
                                  select new { si.Name, si.Qty, si.Price }).ToListAsync();

                topItems = rows.GroupBy(x => x.Name)
                    .Select(g => new { product = g.Key, category = "Продажі", qty = g.Sum(x => x.Qty), sum = g.Sum(x => (decimal)x.Price * x.Qty) })
                    .OrderByDescending(x => x.sum)
                    .Take(10)
                    .Cast<object>()
                    .ToList();
            }

            return Ok(new
            {
                period = new { from = startLocal.ToString("yyyy-MM-dd"), to = endLE.AddDays(-1).ToString("yyyy-MM-dd"), type = mode },
                kpi    = new { income, salesCount, repairsCount = repCount, avgCheck, profitEstimate = income, newClients = newCl },
                topProducts = topItems
            });
        }

        [HttpGet("report-pdf")]
        public async Task<IActionResult> ReportPdf([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type)
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

            var income = salesRev + repRev;

            QuestPDF.Settings.License = LicenseType.Community;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("Kontakt — Зведений звіт").FontSize(20).Bold().AlignCenter();
                        col.Item().Text($"Період: {startLocal:dd.MM.yyyy} — {endLE.AddDays(-1):dd.MM.yyyy}").FontSize(14).AlignCenter();
                        col.Item().PaddingTop(10).LineHorizontal(1);
                    });
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text("KPI").Bold().FontSize(16);
                        col.Item().Text($"Загальний дохід: {income:N2} грн");
                        col.Item().Text($"Продажів: {salesCount} (₴{salesRev:N2})");
                        col.Item().Text($"Ремонтів: {repCount} (₴{repRev:N2})");
                    });
                    page.Footer().AlignCenter().Text($"Згенеровано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
            return File(pdf.GeneratePdf(), "application/pdf", "report.pdf");
        }
    }
}
