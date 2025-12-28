using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contact.API.Models
{
    [Table("repairs")]
    public class Repair
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required, MaxLength(100)]
        public string DeviceType { get; set; } = "";

        [Required, MaxLength(150)]
        public string Model { get; set; } = "";

        [Required, MaxLength(500)]
        public string Problem { get; set; } = "";

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Новий";

        [Required]
        public string PartsUsed { get; set; } = "";

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
