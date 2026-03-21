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
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(AppDbContext context, ILogger<ReceiptsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/receipts/sale/{id}/pdf
        [HttpGet("sale/{id}/pdf")]
        public async Task<IActionResult> GetSaleReceiptPdf(int id)
        {
            var sale = await _context.SaleHeaders
                .Include(h => h.Items)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (sale == null) return NotFound();

            var client = await _context.Clients.FindAsync(sale.ClientId);

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
                        col.Item().Text("Сервісний центр Kontakt")
                            .FontSize(20).Bold().AlignCenter();
                        col.Item().Text($"Чек №{sale.Id}")
                            .FontSize(14).AlignCenter();
                        col.Item().PaddingTop(10).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Дата: {sale.Date:dd.MM.yyyy}");
                        col.Item().Text($"Клієнт: {client?.FullName ?? "Невідомо"}");
                        col.Item().Text($"Спосіб оплати: {sale.Payment}");
                        col.Item().Text($"Статус: {sale.Status}");
                        col.Item().PaddingTop(10).Text("Товари/Послуги:").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Назва").Bold();
                                header.Cell().Text("Кіл-ть").Bold();
                                header.Cell().Text("Ціна").Bold();
                                header.Cell().Text("Сума").Bold();
                            });

                            foreach (var item in sale.Items)
                            {
                                table.Cell().Text(item.Name);
                                table.Cell().Text(item.Qty.ToString());
                                table.Cell().Text($"{item.Price:0.00} грн");
                                table.Cell().Text($"{item.Price * item.Qty:0.00} грн");
                            }
                        });

                        col.Item().PaddingTop(10)
                            .Text($"Разом: {sale.Total:0.00} грн").Bold().FontSize(14);
                    });

                    page.Footer().AlignCenter()
                        .Text($"Дякуємо за покупку! {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });

            var bytes = pdf.GeneratePdf();
            _logger.LogInformation("Згенеровано PDF чек для продажу. Id: {Id}", id);
            return File(bytes, "application/pdf", $"receipt-sale-{id}.pdf");
        }

        // GET: api/receipts/repair/{id}/pdf
        [HttpGet("repair/{id}/pdf")]
        public async Task<IActionResult> GetRepairReceiptPdf(int id)
        {
            var repair = await _context.Repairs.FindAsync(id);
            if (repair == null) return NotFound();

            var client = await _context.Clients.FindAsync(repair.ClientId);

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Сервісний центр Kontakt")
                            .FontSize(20).Bold().AlignCenter();
                        col.Item().Text($"Акт виконаних робіт №{repair.Id}")
                            .FontSize(14).AlignCenter();
                        col.Item().PaddingTop(10).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Дата: {repair.CreatedAt:dd.MM.yyyy}");
                        col.Item().Text($"Клієнт: {client?.FullName ?? "Невідомо"}");
                        col.Item().Text($"Пристрій: {repair.DeviceType} {repair.Model}");
                        col.Item().Text($"Проблема: {repair.Problem}");
                        col.Item().Text($"Статус: {repair.Status}");
                        col.Item().Text($"Використані запчастини: {repair.PartsUsed}");
                        col.Item().PaddingTop(10)
                            .Text($"Вартість: {repair.TotalCost:0.00} грн").Bold().FontSize(14);
                    });

                    page.Footer().AlignCenter()
                        .Text($"Дякуємо за звернення! {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });

            var bytes = pdf.GeneratePdf();
            _logger.LogInformation("Згенеровано PDF акт для ремонту. Id: {Id}", id);
            return File(bytes, "application/pdf", $"receipt-repair-{id}.pdf");
        }
    }
}