using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; 
namespace Contact.API.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [JsonIgnore] 
        public string History { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}