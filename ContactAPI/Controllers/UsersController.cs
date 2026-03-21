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
            {
                _logger.LogWarning("Спроба скинути пароль — користувача не знайдено: {Username}", req.Username);
                return NotFound("Користувача не знайдено або email не співпадає.");
            }

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Пароль скинуто для користувача: {Username}", req.Username);
            return Ok(new { message = "Пароль оновлено" });
        }
    }

    public record ResetPasswordRequest(string Username, string Email, string NewPassword);
}