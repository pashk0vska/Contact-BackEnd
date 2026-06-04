using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Contact.API.Data;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin,master")]
    public class MastersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public MastersController(AppDbContext db) => _db = db;

        // GET /api/masters — список майстрів (користувачі з роллю "master") для випадаючих списків
        [HttpGet]
        public async Task<IActionResult> GetMasters()
        {
            var masters = await _db.Users.AsNoTracking()
                .Where(u => u.Role == "master")
                .OrderBy(u => u.Username)
                .Select(u => new { id = u.Id, name = u.Username })
                .ToListAsync();
            return Ok(masters);
        }
    }
}
