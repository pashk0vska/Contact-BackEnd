using Contact.API.Data;
using Contact.API.Helpers;
using Contact.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        private string? GetCallerRole() =>
            User.FindFirstValue(ClaimTypes.Role);

        private bool IsSuperAdmin() => GetCallerRole() == "superadmin";
        private bool IsAdmin()      => GetCallerRole() == "admin";
        private bool IsAdminOrSuper() => IsSuperAdmin() || IsAdmin();

        // GET /api/users — superadmin та admin бачать список
        [HttpGet]
        [Authorize(Roles = "superadmin,admin")]
        public IActionResult GetAll()
        {
            var users = _context.Users
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToList();
            return Ok(users);
        }

        // POST /api/users — створення користувача
        // superadmin → може створювати admin та master
        // admin      → може створювати тільки master
        [HttpPost]
        [Authorize(Roles = "superadmin,admin")]
        public IActionResult CreateUser([FromBody] CreateUserRequest req)
        {
            if (req == null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required.");
            if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest("Password is required.");
            if (string.IsNullOrWhiteSpace(req.Email))    return BadRequest("Email is required.");

            var callerRole = GetCallerRole();
            var targetRole = (req.Role ?? "master").ToLower().Trim();

            // Валідація ролі відносно калера
            if (callerRole == "admin")
            {
                // admin може створювати тільки master
                if (targetRole != "master")
                    return StatusCode(403, "Admin може створювати тільки користувачів з роллю 'master'.");
            }
            else if (callerRole == "superadmin")
            {
                // superadmin може створювати admin або master, але не ще одного superadmin
                if (targetRole == "superadmin")
                    return StatusCode(403, "Не можна створити ще одного superadmin.");
                if (targetRole != "admin" && targetRole != "master")
                    return StatusCode(403, "Невалідна роль. Допустимі: 'admin', 'master'.");
            }

            if (_context.Users.Any(u => u.Username == req.Username))
                return Conflict(new { message = "Користувач із таким логіном уже існує." });

            var user = new User
            {
                Username     = req.Username,
                Email        = req.Email,
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role         = targetRole
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _logger.LogInformation("User {Caller} created user {Username} with role {Role}",
                User.FindFirstValue(ClaimTypes.Name), user.Username, user.Role);

            return Ok(new { user.Id, user.Username, user.Email, user.Role });
        }

        // DELETE /api/users/{id}
        // superadmin може видаляти admin та master (але не самого себе та не інших superadmin)
        // admin може видаляти тільки master
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var target = await _context.Users.FindAsync(id);
            if (target == null) return NotFound("Користувача не знайдено.");

            // superadmin не можна видаляти через API ніколи
            if (target.Role == "superadmin")
                return StatusCode(403, "Superadmin не може бути видалений через API.");

            var callerRole = GetCallerRole();
            var callerUsername = User.FindFirstValue(ClaimTypes.Name);

            if (callerRole == "admin")
            {
                // admin може видаляти тільки master
                if (target.Role != "master")
                    return StatusCode(403, "Admin може видаляти тільки користувачів з роллю 'master'.");
            }
            else if (callerRole == "superadmin")
            {
                // superadmin не може видалити сам себе
                if (target.Username == callerUsername)
                    return StatusCode(403, "Не можна видалити власний акаунт.");
            }

            _context.Users.Remove(target);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Caller} deleted user {Username} (role: {Role})",
                callerUsername, target.Username, target.Role);

            return NoContent();
        }

        // PUT /api/users/{id}/role — зміна ролі (тільки superadmin)
        [HttpPut("{id:int}/role")]
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest req)
        {
            var target = await _context.Users.FindAsync(id);
            if (target == null) return NotFound("Користувача не знайдено.");

            if (target.Role == "superadmin")
                return StatusCode(403, "Роль superadmin не можна змінити.");

            var newRole = (req.Role ?? "").ToLower().Trim();
            if (newRole != "admin" && newRole != "master")
                return BadRequest("Допустимі ролі: 'admin', 'master'.");

            target.Role = newRole;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Superadmin changed role of {Username} to {Role}", target.Username, newRole);
            return Ok(new { target.Id, target.Username, target.Role });
        }

        // POST /api/users/register — публічна реєстрація (вимкнено в продакшені, тільки через адмінів)
        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "Заповніть всі поля." });

            if (_context.Users.Any(u => u.Username == req.Username))
                return Conflict(new { message = "Користувач із таким логіном уже існує." });

            var rawKeys    = new List<string>();
            var hashedKeys = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var key = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
                rawKeys.Add(key);
                hashedKeys.Add(BCrypt.Net.BCrypt.HashPassword(key, workFactor: 10));
            }

            var user = new User
            {
                Username     = req.Username,
                Email        = req.Email,
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role         = "master", // публічна реєстрація дає роль master
                RecoveryKeys = string.Join("|", hashedKeys)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _logger.LogInformation("Registered user: {Username}", user.Username);
            return Ok(new { message = "OK", user.Id, user.Username, user.Email, user.Role, recoveryKeys = rawKeys });
        }

        // POST /api/users/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Заповніть всі поля.");
            if (string.IsNullOrWhiteSpace(req.RecoveryKey))
                return BadRequest("Введіть резервний ключ.");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
            if (user == null) return NotFound("Користувача не знайдено.");

            var storedKeys = (user.RecoveryKeys ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);
            bool keyValid  = false;
            foreach (var hk in storedKeys)
            {
                try { if (BCrypt.Net.BCrypt.Verify(req.RecoveryKey.Trim().ToUpper(), hk)) { keyValid = true; break; } } catch { }
            }
            if (!keyValid) return StatusCode(403, "Невірний резервний ключ.");

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset for: {Username}", req.Username);
            return Ok(new { message = "Пароль оновлено" });
        }
    }

    public record RegisterRequest(string Username, string Email, string Password);
    public record ResetPasswordRequest(string Username, string NewPassword, string? RecoveryKey);
    public record ChangeRoleRequest(string Role);

    public class CreateUserRequest
    {
        public string Username { get; set; } = "";
        public string Email    { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Role    { get; set; }
    }
}
