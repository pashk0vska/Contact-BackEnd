using Contact.API.Data;
using Contact.API.Helpers;
using Contact.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        // POST: api/users (створення користувача)
        [HttpPost]
        public IActionResult CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Некоректні дані користувача.");

            if (_context.Users.Any(u => u.Username == user.Username))
                return Conflict("Користувач із таким логіном уже існує.");

            // Використовуємо PasswordHasher замість дубльованого приватного методу
            user.PasswordHash = PasswordHasher.Hash(user.PasswordHash);

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Користувача успішно створено.",
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        // POST: api/users/reset-password (оновлення пароля)
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                return NotFound("Користувача не знайдено.");

            // Використовуємо PasswordHasher замість дубльованого приватного методу
            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Пароль оновлено" });
        }

        // HashPassword() ВИДАЛЕНО — використовуємо PasswordHasher.Hash() (ліквідація дублювання)
    }

    // DTO для запиту reset-password
    public record ResetPasswordRequest(string Username, string NewPassword);
}
