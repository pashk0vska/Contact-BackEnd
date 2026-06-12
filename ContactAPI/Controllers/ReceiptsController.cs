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
    [Authorize(Roles = "superadmin,admin,master")]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(AppDbContext context, ILogger<ReceiptsController> logger)
        {
            _context = context; _logger = logger;
        }

        const string ShopName = "КОНТАКТ";
        const string ShopSub  = "Сервісний центр · ФОП Марціновський О.В.";
        const string ShopAddr = "м. Коломия, вул. Валова 36В · +380 96 664 30 00";

        [HttpGet("sale/{id}/pdf")]
        public async Task<IActionResult> GetSaleReceiptPdf(int id)
        {
            var sale = await _context.SaleHeaders.Include(h => h.Items).FirstOrDefaultAsync(h => h.Id == id);
            if (sale == null) return NotFound();
            var client = await _context.Clients.FindAsync(sale.ClientId);

            QuestPDF.Settings.License = LicenseType.Community;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5); page.Margin(1.4f, Unit.Centimetre); page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Column(col =>
                    {
                        col.Item().Text(ShopName).FontSize(26).Bold().AlignCenter();
                        col.Item().Text(ShopSub).FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                        col.Item().Text(ShopAddr).FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                        col.Item().PaddingTop(8).LineHorizontal(1);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text($"Чек № S-{sale.Id:0000}").SemiBold();
                            r.RelativeItem().AlignRight().Text($"{sale.Date.ToLocalTime():dd.MM.yyyy HH:mm}").FontColor(Colors.Grey.Medium);
                        });
                    });
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Клієнт: {client?.FullName ?? "—"}");
                        col.Item().PaddingBottom(8).Text($"Оплата: {sale.Payment}");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });
                            table.Header(h =>
                            {
                                h.Cell().BorderBottom(1).PaddingBottom(4).Text("Найменування").SemiBold();
                                h.Cell().BorderBottom(1).PaddingBottom(4).AlignCenter().Text("К-ть").SemiBold();
                                h.Cell().BorderBottom(1).PaddingBottom(4).AlignRight().Text("Ціна").SemiBold();
                                h.Cell().BorderBottom(1).PaddingBottom(4).AlignRight().Text("Сума").SemiBold();
                            });
                            foreach (var item in sale.Items)
                            {
                                table.Cell().PaddingVertical(3).Text(item.Name);
                                table.Cell().PaddingVertical(3).AlignCenter().Text(item.Qty.ToString());
                                table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Price:0.00}");
                                table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Price * item.Qty:0.00}");
                            }
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("До сплати").Bold().FontSize(14);
                            r.RelativeItem().AlignRight().Text($"{sale.Total:0.00} ₴").Bold().FontSize(14);
                        });
                    });
                    page.Footer().AlignCenter().Text("Дякуємо за звернення!").FontColor(Colors.Grey.Medium);
                });
            });
            return File(pdf.GeneratePdf(), "application/pdf", $"receipt-sale-{id}.pdf");
        }

        [HttpGet("repair/{id}/pdf")]
        public async Task<IActionResult> GetRepairReceiptPdf(int id)
        {
            var repair = await _context.Repairs.FindAsync(id);
            if (repair == null) return NotFound();
            var client = await _context.Clients.FindAsync(repair.ClientId);

            QuestPDF.Settings.License = LicenseType.Community;
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5); page.Margin(1.4f, Unit.Centimetre); page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Column(col =>
                    {
                        col.Item().Text(ShopName).FontSize(26).Bold().AlignCenter();
                        col.Item().Text(ShopSub).FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                        col.Item().Text(ShopAddr).FontSize(10).FontColor(Colors.Grey.Medium).AlignCenter();
                        col.Item().PaddingTop(8).LineHorizontal(1);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text($"Акт № R-{repair.Id:0000}").SemiBold();
                            r.RelativeItem().AlignRight().Text($"{repair.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}").FontColor(Colors.Grey.Medium);
                        });
                    });
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Клієнт: {client?.FullName ?? "—"}");
                        col.Item().Text($"Пристрій: {repair.DeviceType} {repair.Model}".Trim());
                        col.Item().Text($"Несправність: {repair.Problem}");
                        if (!string.IsNullOrWhiteSpace(repair.PartsUsed))
                            col.Item().Text($"Запчастини / роботи: {repair.PartsUsed}");
                        col.Item().PaddingTop(10).LineHorizontal(1);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Вартість").Bold().FontSize(14);
                            r.RelativeItem().AlignRight().Text($"{repair.TotalCost:0.00} ₴").Bold().FontSize(14);
                        });
                    });
                    page.Footer().AlignCenter().Text("Дякуємо за звернення!").FontColor(Colors.Grey.Medium);
                });
            });
            return File(pdf.GeneratePdf(), "application/pdf", $"receipt-repair-{id}.pdf");
        }
    }
}
