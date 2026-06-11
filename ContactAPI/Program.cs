using Contact.API.Data;
using Contact.API.Helpers;
using Contact.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Contact API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
      ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Connection string not configured.");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime         = true,
            ValidIssuer              = jwt["Issuer"],
            ValidAudience            = jwt["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"] ?? "dev_super_secret_key_change_me")),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Database initialization & superadmin seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Безпечне додавання колонок (працює і на MySQL 8, і на MariaDB).
    // ПРИМІТКА: 'ALTER TABLE ... ADD COLUMN IF NOT EXISTS' — синтаксис MariaDB,
    // у MySQL 8 він падає. Тому перевіряємо наявність колонки через information_schema.
    EnsureColumn(db, "users",        "RecoveryKeys", "longtext NULL");
    EnsureColumn(db, "repairs",      "MasterId",     "int NULL");        // T1
    EnsureColumn(db, "sale_headers", "MasterId",     "int NULL");        // T1
    EnsureColumn(db, "sale_items",   "Type",         "varchar(20) NULL");   // T6
    EnsureColumn(db, "clients",      "Source",       "varchar(20) NULL");   // Інтеграція з Конфігуратором ПК: походження клієнта (crm/configurator)

    // Міграція: оновити роль "user" -> "master" для існуючих користувачів
    try { db.Database.ExecuteSqlRaw("UPDATE users SET Role = 'master' WHERE Role = 'user'"); } catch { }

    // Бекфіл: наявні клієнти без джерела вважаються створеними в CRM.
    // Ідемпотентно: після першого запуску NULL-рядків не лишається, рядки Конфігуратора ('configurator') не чіпаються.
    try { db.Database.ExecuteSqlRaw("UPDATE clients SET Source = 'crm' WHERE Source IS NULL OR Source = ''"); } catch { }

    // Seed superadmin (БЕЗ авто-промоушену admin -> superadmin)
    SeedSuperAdmin(db);
}

app.UseCors("dev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// --- Idempotent column add, сумісне з MySQL 8 та MariaDB ---
static void EnsureColumn(AppDbContext db, string table, string column, string ddl)
{
    try
    {
        var conn = db.Database.GetDbConnection();
        bool opened = false;
        if (conn.State != System.Data.ConnectionState.Open) { conn.Open(); opened = true; }
        try
        {
            int exists;
            using (var check = conn.CreateCommand())
            {
                check.CommandText =
                    "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t AND COLUMN_NAME = @c";
                var pt = check.CreateParameter(); pt.ParameterName = "@t"; pt.Value = table;  check.Parameters.Add(pt);
                var pc = check.CreateParameter(); pc.ParameterName = "@c"; pc.Value = column; check.Parameters.Add(pc);
                exists = Convert.ToInt32(check.ExecuteScalar());
            }
            if (exists == 0)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE `{table}` ADD COLUMN `{column}` {ddl}";
                alter.ExecuteNonQuery();
                Console.WriteLine($"[Migrate] Added column {table}.{column}");
            }
        }
        finally { if (opened) conn.Close(); }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migrate] {table}.{column} failed: {ex.Message}");
    }
}

// --- Seed superadmin ---
static void SeedSuperAdmin(AppDbContext db)
{
    // Якщо superadmin уже є — нічого не робимо.
    var existing = db.Users.FirstOrDefault(u => u.Role == "superadmin");
    if (existing != null) return;

    // ВАЖЛИВО: НЕ промоутимо існуючого admin до superadmin —
    // інакше ролі superadmin та admin "злипаються" в один акаунт.
    var superadmin = new User
    {
        Username     = "superadmin",
        Email        = "superadmin@kontakt.local",
        PasswordHash = PasswordHasher.Hash("SuperAdmin123!"),
        Role         = "superadmin"
    };
    db.Users.Add(superadmin);
    db.SaveChanges();
    Console.WriteLine("[Seed] Created default superadmin (username: superadmin, password: SuperAdmin123!)");
}
