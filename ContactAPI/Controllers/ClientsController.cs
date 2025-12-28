using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;
using Contact.API.Models;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ClientsController(AppDbContext db) => _db = db;

        // DTO для відповіді в списку
        public record ClientListItemDto(int Id, string FullName, string Phone, string Email);

        // GET /api/Clients?q=&sort=FullName&dir=asc&page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q = null,
            [FromQuery] string sort = "FullName",
            [FromQuery] string dir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)


        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            IQueryable<Client> query = _db.Clients.AsNoTracking();

            // Пошук
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    EF.Functions.Like(c.FullName.ToLower(), $"%{term}%") ||
                    EF.Functions.Like(c.Phone.ToLower(), $"%{term}%") ||
                    EF.Functions.Like(c.Email.ToLower(), $"%{term}%")
                );
            }

            // Сортування
            bool desc = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "phone" => desc ? query.OrderByDescending(x => x.Phone).ThenBy(x => x.Id)
                                : query.OrderBy(x => x.Phone).ThenBy(x => x.Id),
                "email" => desc ? query.OrderByDescending(x => x.Email).ThenBy(x => x.Id)
                                : query.OrderBy(x => x.Email).ThenBy(x => x.Id),
                _ => desc ? query.OrderByDescending(x => x.FullName).ThenBy(x => x.Id)
                                : query.OrderBy(x => x.FullName).ThenBy(x => x.Id),
            };

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClientListItemDto(c.Id, c.FullName, c.Phone, c.Email))
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }

        // POST /api/Clients
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Client model)
        {
            if (model == null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(model.FullName)) return BadRequest("FullName is required.");
            if (string.IsNullOrWhiteSpace(model.Phone)) return BadRequest("Phone is required.");
            model.Email ??= string.Empty;

            _db.Clients.Add(model);
            await _db.SaveChangesAsync();
            return Ok(new { id = model.Id });
        }

        // PUT /api/Clients/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] Client model)
        {
            var entity = await _db.Clients.FindAsync(id);
            if (entity == null) return NotFound();

            entity.FullName = model.FullName?.Trim() ?? entity.FullName;
            entity.Phone = model.Phone?.Trim() ?? entity.Phone;
            entity.Email = model.Email?.Trim() ?? entity.Email;

            await _db.SaveChangesAsync();
            return NoContent();


        }

        // DELETE /api/Clients/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {

            var entity = await _db.Clients.FindAsync(id);
            if (entity == null) return NotFound();

            _db.Clients.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
