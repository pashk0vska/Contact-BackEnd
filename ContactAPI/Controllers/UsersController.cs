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
