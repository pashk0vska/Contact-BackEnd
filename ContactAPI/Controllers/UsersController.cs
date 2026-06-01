<<<<<<< HEAD
using Contact.API.Data; using Contact.API.Helpers; using Contact.API.Models; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace Contact.API.Controllers
{
    [ApiController][Route("api/[controller]")][Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context; private readonly ILogger<UsersController> _logger;
        public UsersController(AppDbContext context,ILogger<UsersController> logger){_context=context;_logger=logger;}

        [HttpGet][Authorize(Roles="admin")] public IActionResult GetAll()=>Ok(_context.Users.Select(u=>new{u.Id,u.Username,u.Email,u.Role}).ToList());

        [HttpPost][Authorize(Roles="admin")]
        public IActionResult CreateUser([FromBody] User user){
            if(user==null)return BadRequest();if(_context.Users.Any(u=>u.Username==user.Username))return Conflict("Існує.");
            if(string.IsNullOrWhiteSpace(user.Role)||user.Role.ToLower()=="admin")user.Role="user";
            user.PasswordHash=PasswordHasher.Hash(user.PasswordHash);_context.Users.Add(user);_context.SaveChanges();
            return Ok(new{user.Id,user.Username,user.Email,user.Role});
        }

        [HttpPost("register")][AllowAnonymous]
        public IActionResult Register([FromBody] RegisterRequest req){
            if(string.IsNullOrWhiteSpace(req.Username)||string.IsNullOrWhiteSpace(req.Password)||string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new{message="Заповніть всі поля."});
            if(_context.Users.Any(u=>u.Username==req.Username))
                return Conflict(new{message="Користувач із таким логіном уже існує."});
            // Generate 3 recovery keys
            var rawKeys = new List<string>();
            var hashedKeys = new List<string>();
            for(int i=0;i<3;i++){
                var key = Guid.NewGuid().ToString("N").Substring(0,12).ToUpper();
                rawKeys.Add(key);
                hashedKeys.Add(BCrypt.Net.BCrypt.HashPassword(key, workFactor:10));
            }
            var user = new User{Username=req.Username,Email=req.Email,PasswordHash=PasswordHasher.Hash(req.Password),Role="user",RecoveryKeys=string.Join("|",hashedKeys)};
            _context.Users.Add(user);_context.SaveChanges();
            _logger.LogInformation("Registered user: {Username}",user.Username);
            return Ok(new{message="OK",user.Id,user.Username,user.Email,user.Role,recoveryKeys=rawKeys});
        }

        [HttpPost("reset-password")][AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req){
            if(string.IsNullOrWhiteSpace(req.Username)||string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Заповніть всі поля.");
            if(string.IsNullOrWhiteSpace(req.RecoveryKey))
                return BadRequest("Введіть резервний ключ.");
            var user=await _context.Users.SingleOrDefaultAsync(u=>u.Username==req.Username);
            if(user==null) return NotFound("Користувача не знайдено.");
            // Validate recovery key
            var storedKeys = (user.RecoveryKeys ?? "").Split('|',StringSplitOptions.RemoveEmptyEntries);
            bool keyValid = false;
            foreach(var hk in storedKeys){
                try{ if(BCrypt.Net.BCrypt.Verify(req.RecoveryKey.Trim().ToUpper(), hk)){keyValid=true;break;} }catch{}
            }
            if(!keyValid) return StatusCode(403,"Невірний резервний ключ.");
            user.PasswordHash=PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset for: {Username}",req.Username);
            return Ok(new{message="Пароль оновлено"});
        }
    }
    public record RegisterRequest(string Username,string Email,string Password);
    public record ResetPasswordRequest(string Username, string NewPassword, string? RecoveryKey);
}
=======
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

        // GET: api/users — superadmin бачить всіх, admin бачить тільки master
        [HttpGet]
        [Authorize(Roles = "superadmin,admin")]
        public IActionResult GetAll()
        {
            var currentRole = User.FindFirstValue(ClaimTypes.Role);
            _logger.LogInformation("Отримання списку користувачів. Роль: {Role}", currentRole);

            var query = _context.Users.AsQueryable();

            // Admin бачить тільки майстрів
            if (currentRole == "admin")
                query = query.Where(u => u.Role == "master");

            var users = query
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToList();

            return Ok(users);
        }

        // POST: api/users — superadmin створює admin/master, admin створює тільки master
        [HttpPost]
        [Authorize(Roles = "superadmin,admin")]
        public IActionResult CreateUser([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Некоректні дані користувача.");

            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Заповніть всі поля.");

            if (_context.Users.Any(u => u.Username == user.Username))
            {
                _logger.LogWarning("Спроба створити користувача з існуючим логіном: {Username}", user.Username);
                return Conflict("Користувач із таким логіном уже існує.");
            }

            var currentRole = User.FindFirstValue(ClaimTypes.Role);
            var requestedRole = (user.Role ?? "").ToLower();

            // Заборонити створення superadmin через API взагалі
            if (requestedRole == "superadmin")
                return BadRequest("Неможливо створити superadmin через API.");

            // Admin може створювати тільки master
            if (currentRole == "admin" && requestedRole != "master")
                return Forbid();

            // Якщо роль не вказана або невалідна — встановити master за замовчуванням
            if (requestedRole != "admin" && requestedRole != "master")
                user.Role = "master";

            user.PasswordHash = PasswordHasher.Hash(user.PasswordHash);
            _context.Users.Add(user);
            _context.SaveChanges();

            _logger.LogInformation("Створено нового користувача: {Username}, роль: {Role}", user.Username, user.Role);
            return Ok(new { message = "Користувача успішно створено.", user.Id, user.Username, user.Email, user.Role });
        }

        // DELETE: api/users/{id} — тільки superadmin може видаляти
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Користувача не знайдено.");

            // Заборонити видалення superadmin
            if (user.Role == "superadmin")
                return BadRequest("Неможливо видалити superadmin.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Видалено користувача: {Username}", user.Username);
            return NoContent();
        }

        // PUT: api/users/{id} — superadmin може міняти роль будь-кому, admin тільки master
        [HttpPut("{id:int}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            // Заборонити редагувати superadmin
            if (user.Role == "superadmin")
                return BadRequest("Неможливо редагувати superadmin.");

            // Admin може редагувати тільки master
            if (currentRole == "admin" && user.Role != "master")
                return Forbid();

            // Заборонити підвищити до superadmin
            if ((dto.Role ?? "").ToLower() == "superadmin")
                return BadRequest("Неможливо призначити роль superadmin.");

            user.Email = dto.Email ?? user.Email;
            if (!string.IsNullOrWhiteSpace(dto.PasswordHash))
                user.PasswordHash = PasswordHasher.Hash(dto.PasswordHash);
            if (!string.IsNullOrWhiteSpace(dto.Role))
                user.Role = dto.Role.ToLower();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Оновлено користувача: {Username}", user.Username);
            return NoContent();
        }

        // POST: api/users/reset-password — публічне скидання паролю
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Заповніть всі поля.");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
            {
                _logger.LogWarning("Спроба скинути пароль — не знайдено: {Username}", req.Username);
                return NotFound("Користувача не знайдено.");
            }

            user.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Пароль скинуто: {Username}", req.Username);
            return Ok(new { message = "Пароль оновлено" });
        }
    }

    public record RegisterRequest(string Username, string Email, string Password);
    public record ResetPasswordRequest(string Username, string NewPassword);
}
>>>>>>> f98bf5a (chore: cleanup gitignore, remove build artifacts)
