using System.ComponentModel.DataAnnotations;
namespace Contact.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string? Role { get; set; }
        public string? RecoveryKeys { get; set; } // comma-separated hashed keys
    }
}
