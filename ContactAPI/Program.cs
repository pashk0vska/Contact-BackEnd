using Contact.API.Data; using Microsoft.AspNetCore.Authentication.JwtBearer; using Microsoft.EntityFrameworkCore; using Microsoft.IdentityModel.Tokens; using System.Text;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c=>{c.SwaggerDoc("v1",new(){Title="Contact API",Version="v1"});c.AddSecurityDefinition("Bearer",new Microsoft.OpenApi.Models.OpenApiSecurityScheme{Name="Authorization",Type=Microsoft.OpenApi.Models.SecuritySchemeType.Http,Scheme="Bearer",BearerFormat="JWT",In=Microsoft.OpenApi.Models.ParameterLocation.Header});c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement{{new Microsoft.OpenApi.Models.OpenApiSecurityScheme{Reference=new Microsoft.OpenApi.Models.OpenApiReference{Type=Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,Id="Bearer"}},Array.Empty<string>()}});});
var cs=builder.Configuration.GetConnectionString("DefaultConnection")??Environment.GetEnvironmentVariable("CONNECTION_STRING");
if(string.IsNullOrWhiteSpace(cs))throw new InvalidOperationException("Connection string not configured.");
builder.Services.AddDbContext<AppDbContext>(opt=>{opt.UseMySql(cs,ServerVersion.AutoDetect(cs));});
builder.Services.AddCors(opt=>{opt.AddPolicy("dev",p=>p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());});
builder.Services.AddControllers();
var jwt=builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>{options.RequireHttpsMetadata=false;options.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateIssuerSigningKey=true,ValidateLifetime=true,ValidIssuer=jwt["Issuer"],ValidAudience=jwt["Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]??"dev_super_secret_key_change_me"))};});
builder.Services.AddAuthorization();
var app=builder.Build();
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}
if(app.Environment.IsDevelopment()){using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.Database.EnsureCreated();try{db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS RecoveryKeys longtext NULL");}catch{}}
app.UseCors("dev");app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.Run();
