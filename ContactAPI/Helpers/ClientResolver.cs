using Contact.API.Data; using Contact.API.Models; using Microsoft.EntityFrameworkCore;
namespace Contact.API.Helpers
{
    public static class ClientResolver
    {
<<<<<<< HEAD
        public class ResolveResult { public bool Success { get; set; } public int ClientId { get; set; } public string? ErrorMessage { get; set; } }
        public static async Task<ResolveResult> ResolveOrCreateAsync(AppDbContext db, int? clientId, string? clientName, string? clientPhone = null)
        {
            if (clientId.GetValueOrDefault() > 0) return new ResolveResult { Success = true, ClientId = clientId!.Value };
            var name = (clientName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return new ResolveResult { Success = false, ErrorMessage = "Client is required" };
            var existing = await db.Clients.AsNoTracking()
                .Where(c => c.FullName.ToLower() == name.ToLower() || (clientPhone != null && clientPhone != "" && c.Phone == clientPhone))
                .Select(c => new { c.Id }).FirstOrDefaultAsync();
            if (existing != null) return new ResolveResult { Success = true, ClientId = existing.Id };
            var newClient = new Client { FullName = name, Phone = (clientPhone ?? "").Trim(), Email = "", History = "" };
            db.Clients.Add(newClient); await db.SaveChangesAsync();
=======
        public class ResolveResult
        {
            public bool Success { get; set; }
            public int ClientId { get; set; }
            public string? ErrorMessage { get; set; }
        }

        public static async Task<ResolveResult> ResolveOrCreateAsync(
            AppDbContext db, int? clientId, string? clientName, string? clientPhone = null)
        {
            // Якщо передано існуючий Id — використовуємо його
            if (clientId.GetValueOrDefault() > 0)
                return new ResolveResult { Success = true, ClientId = clientId!.Value };

            var name = (clientName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new ResolveResult { Success = false, ErrorMessage = "Client is required" };

            // Шукаємо за ім'ям або телефоном
            var phone = (clientPhone ?? "").Trim();
            var existing = await db.Clients
                .AsNoTracking()
                .Where(c => c.FullName.ToLower() == name.ToLower() ||
                           (!string.IsNullOrWhiteSpace(phone) && c.Phone == phone))
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync();

            if (existing != null)
                return new ResolveResult { Success = true, ClientId = existing.Id };

            // Створюємо нового клієнта
            var newClient = new Client
            {
                FullName = name,
                Phone = phone,
                Email = "",
                History = ""
            };
            db.Clients.Add(newClient);
            await db.SaveChangesAsync();

>>>>>>> f98bf5a (chore: cleanup gitignore, remove build artifacts)
            return new ResolveResult { Success = true, ClientId = newClient.Id };
        }
    }
}