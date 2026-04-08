using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Authorization; using Microsoft.EntityFrameworkCore; using Contact.API.Data; using Contact.API.Models; using System.Text.Json;
namespace Contact.API.Controllers
{
    [ApiController][Route("api/[controller]")][Authorize]
    public class BackupController : ControllerBase
    {
        private readonly AppDbContext _db;
        public BackupController(AppDbContext db)=>_db=db;

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var data=new{
                clients=await _db.Clients.AsNoTracking().ToListAsync(),
                repairs=await _db.Repairs.AsNoTracking().ToListAsync(),
                saleHeaders=await _db.SaleHeaders.AsNoTracking().ToListAsync(),
                saleItems=await _db.SaleItems.AsNoTracking().ToListAsync(),
                services=await _db.Services.AsNoTracking().ToListAsync(),
                users=await _db.Users.AsNoTracking().ToListAsync(),
                exportDate=DateTime.UtcNow
            };
            var json=JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true});
            var bytes=System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes,"application/json","kontakt_backup.json");
        }

        public class BackupData{public List<Client>? clients{get;set;}public List<Repair>? repairs{get;set;}public List<SaleHeader>? saleHeaders{get;set;}public List<SaleItem>? saleItems{get;set;}public List<Service>? services{get;set;}public List<User>? users{get;set;}}

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] BackupData data)
        {
            if(data==null) return BadRequest("No data");
            using var transaction=await _db.Database.BeginTransactionAsync();
            try{
                // Clear existing data (order matters for FK)
                _db.SaleItems.RemoveRange(_db.SaleItems);_db.SaleHeaders.RemoveRange(_db.SaleHeaders);_db.Repairs.RemoveRange(_db.Repairs);_db.Services.RemoveRange(_db.Services);_db.Clients.RemoveRange(_db.Clients);
                await _db.SaveChangesAsync();
                // Re-insert
                if(data.clients!=null)_db.Clients.AddRange(data.clients);
                if(data.services!=null)_db.Services.AddRange(data.services);
                await _db.SaveChangesAsync();
                if(data.repairs!=null)_db.Repairs.AddRange(data.repairs);
                if(data.saleHeaders!=null){foreach(var h in data.saleHeaders)h.Items=new();_db.SaleHeaders.AddRange(data.saleHeaders);}
                await _db.SaveChangesAsync();
                if(data.saleItems!=null){foreach(var i in data.saleItems)i.Sale=null;_db.SaleItems.AddRange(data.saleItems);}
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new{message="Restored successfully"});
            }catch(Exception ex){await transaction.RollbackAsync();return StatusCode(500,$"Restore failed: {ex.Message}");}
        }
    }
}
