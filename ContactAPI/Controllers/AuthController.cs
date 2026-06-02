using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Contact.API.Data;
using Contact.API.Helpers;
using Contact.API.Models;

namespace Contact.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
                return BadRequest("Введіть логін і пароль.");

            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null) return Unauthorized("Користувача не знайдено");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Невірний пароль");

            return Ok(new
            {
                accessToken = GenerateJwtToken(user),
                username    = user.Username,
                role        = user.Role
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "master"),
                new Claim("userId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.Now.AddMinutes(Convert.ToDouble(jwt["ExpiresInMinutes"])),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
