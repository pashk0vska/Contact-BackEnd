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

    // Міграція: додати колонку RecoveryKeys якщо не існує
    try { db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS RecoveryKeys longtext NULL"); } catch { }

    // Міграція (T1): додати колонку MasterId у ремонти та продажі, якщо не існує
    try { db.Database.ExecuteSqlRaw("ALTER TABLE repairs ADD COLUMN IF NOT EXISTS MasterId int NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE sale_headers ADD COLUMN IF NOT EXISTS MasterId int NULL"); } catch { }

    // Міграція: оновити роль "user" → "master" для існуючих користувачів
    try { db.Database.ExecuteSqlRaw("UPDATE users SET Role = 'master' WHERE Role = 'user'"); } catch { }

    // Міграція: оновити перший admin → superadmin (або створити нового)
    SeedSuperAdmin(db);
}

app.UseCors("dev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// --- Seed superadmin ---
static void SeedSuperAdmin(AppDbContext db)
{
    // Перевіряємо, чи є вже superadmin
    var existing = db.Users.FirstOrDefault(u => u.Role == "superadmin");
    if (existing != null) return;

    // Якщо є admin — промоутимо першого до superadmin
    var firstAdmin = db.Users.FirstOrDefault(u => u.Role == "admin");
    if (firstAdmin != null)
    {
        firstAdmin.Role = "superadmin";
        db.SaveChanges();
        Console.WriteLine($"[Seed] Promoted '{firstAdmin.Username}' to superadmin.");
        return;
    }

    // Якщо немає жодного — створюємо superadmin за замовчуванням
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
