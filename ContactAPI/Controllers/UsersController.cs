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
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult GetAll()
        {
            var users = _context.Users
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToList();
            return Ok(users);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Некоректні дані користувача.");

            if (_context.Users.Any(u => u.Username == user.Username))
                return Conflict("Користувач із таким логіном уже існує.");

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

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Заповніть всі поля.");

            var user = await _context.Users.SingleOrDefaultAsync(u =>
                u.Username == req.Username && u.Email == req.Email);

            if (user == null)
                return NotFound("Користувача не знайдено або email не співпадає.");

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пароль оновлено" });
        }
    }

    public record ResetPasswordRequest(string Username, string Email, string NewPassword);
}