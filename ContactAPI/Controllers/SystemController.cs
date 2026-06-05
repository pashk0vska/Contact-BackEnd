using Contact.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin")]
    public class SystemController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SystemController(AppDbContext db) => _db = db;

        // GET /api/System/db-status — короткий стан БД для Налаштувань (T7 / Блок A)
        [HttpGet("db-status")]
        public async Task<IActionResult> DbStatus()
        {
            try
            {
                var conn = _db.Database.GetDbConnection();
                bool opened = false;
                if (conn.State != System.Data.ConnectionState.Open) { await conn.OpenAsync(); opened = true; }

                string version = "";
                int tables = 0;
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT VERSION()";
                        version = (await cmd.ExecuteScalarAsync())?.ToString() ?? "";
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()";
                        tables = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
                finally { if (opened) await conn.CloseAsync(); }

                var records =
                    await _db.Clients.CountAsync()
                    + await _db.Repairs.CountAsync()
                    + await _db.SaleHeaders.CountAsync()
                    + await _db.SaleItems.CountAsync()
                    + await _db.Services.CountAsync()
                    + await _db.Users.CountAsync();

                return Ok(new
                {
                    connected = true,
                    dbName  = conn.Database,
                    server  = conn.DataSource,
                    version,
                    tables,
                    records
                });
            }
            catch (Exception ex)
            {
                return Ok(new { connected = false, error = ex.Message });
            }
        }
    }
}
