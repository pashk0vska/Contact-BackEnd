using Microsoft.EntityFrameworkCore;
using Contact.API.Models;

namespace Contact.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Repair> Repairs => Set<Repair>();
        public DbSet<Client> Clients => Set<Client>();

        // Основні DbSet-и продажів
        public DbSet<SaleHeader> SaleHeaders => Set<SaleHeader>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();

        // АЛІАС для зворотної сумісності з існуючими контролерами:
        // тепер _db.Sales вказує на ту ж таблицю, що і SaleHeaders
        public DbSet<SaleHeader> Sales => Set<SaleHeader>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // таблиці
            b.Entity<User>().ToTable("users");
            b.Entity<Service>().ToTable("services");
            b.Entity<Repair>().ToTable("repairs");
            b.Entity<Client>().ToTable("clients");
            b.Entity<SaleHeader>().ToTable("sale_headers");
            b.Entity<SaleItem>().ToTable("sale_items");

            // FK: sale_headers.ClientId -> clients.Id
            b.Entity<SaleHeader>()
                .HasOne<Client>()
                .WithMany()
                .HasForeignKey(h => h.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Навігація: sale_headers (1) -> sale_items (many)
            b.Entity<SaleHeader>()
                .HasMany(h => h.Items)
                .WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
