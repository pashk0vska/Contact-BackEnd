using Contact.API.Data;
using Contact.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

        // ---------------------- //
        //  GET: api/users
        // ---------------------- //
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        // ---------------------- //
        //  POST: api/users  (створення користувача)
        // ---------------------- //
        [HttpPost]
        public IActionResult CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Некоректні дані користувача.");

            // перевіряємо, чи існує користувач з таким ім’ям
            if (_context.Users.Any(u => u.Username == user.Username))
                return Conflict("Користувач із таким логіном уже існує.");

            // хешуємо пароль перед збереженням
            user.PasswordHash = HashPassword(user.PasswordHash);

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

        // ---------------------- //
        //  POST: api/users/reset-password  (оновлення пароля)
        // ---------------------- //
        [HttpPost("reset-password")]
        [AllowAnonymous] // на час розробки; пізніше можна прибрати
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                return NotFound("Користувача не знайдено.");

            // використовуємо той самий SHA256-хешер, що й при створенні
            user.PasswordHash = HashPassword(req.NewPassword);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Пароль оновлено" });
        }

        // ---------------------- //
        //  Хелпер для хешування
        // ---------------------- //
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    // DTO для запиту reset-password
    public record ResetPasswordRequest(string Username, string NewPassword);
}
