using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contact.API.Models
{
    [Table("services")]
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = "";

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; } = "Repair";

        [Required, MaxLength(500)]
        public string Description { get; set; } = "";
    }
}
