using System.Linq;
using System.Threading.Tasks;
using Contact.API.Data;
using Contact.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Helpers
{
    /// <summary>
    /// Допоміжний клас для пошуку або створення клієнта.
    /// Логіка була продубльована у SalesController.CreateSale() та RepairsController.CreateRepair().
    /// Після рефакторингу — єдине місце відповідальності (SRP).
    /// </summary>
    public static class ClientResolver
    {
        /// <summary>
        /// Результат резолвінгу клієнта.
        /// </summary>
        public class ResolveResult
        {
            public bool Success { get; set; }
            public int ClientId { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// Знаходить існуючого клієнта за Id, або шукає/створює за ім'ям.
        /// Повертає ResolveResult з Id клієнта або повідомленням про помилку.
        /// </summary>
        public static async Task<ResolveResult> ResolveOrCreateAsync(
            AppDbContext db, int? clientId, string? clientName)
        {
            // Якщо передано існуючий Id — використовуємо його
            if (clientId.GetValueOrDefault() > 0)
            {
                return new ResolveResult { Success = true, ClientId = clientId!.Value };
            }

            // Інакше — шукаємо або створюємо за ім'ям
            var name = (clientName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return new ResolveResult { Success = false, ErrorMessage = "Client is required" };
            }

            var existing = await db.Clients
                .AsNoTracking()
                .Where(c => c.FullName.ToLower() == name.ToLower())
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return new ResolveResult { Success = true, ClientId = existing.Id };
            }

            // Створюємо нового клієнта
            var newClient = new Client
            {
                FullName = name,
                Phone = "",
                Email = "",
                History = ""
            };
            db.Clients.Add(newClient);
            await db.SaveChangesAsync();

            return new ResolveResult { Success = true, ClientId = newClient.Id };
        }
    }
}
