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
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult GetAll()
        {
            _logger.LogInformation("Отримання списку користувачів.");
            var users = _context.Users
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToList();
            return Ok(users);
        }

        // POST: api/users — тільки адміни
        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Некоректні дані користувача.");

            if (_context.Users.Any(u => u.Username == user.Username))
            {
                _logger.LogWarning("Спроба створити користувача з існуючим логіном: {Username}", user.Username);
                return Conflict("Користувач із таким логіном уже існує.");
            }

            // Валідація ролі — заборона реєструватись як admin через API
            if (string.IsNullOrWhiteSpace(user.Role) || user.Role.ToLower() == "admin")
                user.Role = "user";

            user.PasswordHash = PasswordHasher.Hash(user.PasswordHash);
            _context.Users.Add(user);
            _context.SaveChanges();

            _logger.LogInformation("Створено нового користувача: {Username}", user.Username);
            return Ok(new
            {
                message = "Користувача успішно створено.",
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        // POST: api/users/register — публічна реєстрація
        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Заповніть всі поля.");

            if (_context.Users.Any(u => u.Username == req.Username))
            {
                _logger.LogWarning("Спроба реєстрації з існуючим логіном: {Username}", req.Username);
                return Conflict("Користувач із таким логіном уже існує.");
            }

            // Роль завжди "user" при публічній реєстрації
            var user = new User
            {
                Username = req.Username,
                Email = req.Email,
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role = "user"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _logger.LogInformation("Зареєстровано нового користувача: {Username}", user.Username);
            return Ok(new
            {
                message = "Реєстрація успішна.",
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        // POST: api/users/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Заповніть всі поля.");

            var user = await _context.Users.SingleOrDefaultAsync(u =>
                u.Username == req.Username);

            if (user == null)
            {
                _logger.LogWarning("Спроба скинути пароль — користувача не знайдено: {Username}", req.Username);
                return NotFound("Користувача не знайдено.");
            }

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Пароль скинуто для користувача: {Username}", req.Username);
            return Ok(new { message = "Пароль оновлено" });
        }
    }

    public record RegisterRequest(string Username, string Email, string Password);
    public record ResetPasswordRequest(string Username, string NewPassword);
}