using System.ComponentModel.DataAnnotations; using System.Text.Json.Serialization;
namespace Contact.API.Models { public class Client { [Key] public int Id { get; set; } [Required] public string FullName { get; set; } = ""; [Required] public string Phone { get; set; } = ""; public string Email { get; set; } = ""; [JsonIgnore] public string History { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Походження запису клієнта: "crm" — створено в CRM; "configurator" — створено в Конфігураторі ПК.
        // Використовується для позначки «К» у списку клієнтів. Дефолт — "crm"; Конфігуратор виставляє "configurator".
        [MaxLength(20)] public string Source { get; set; } = "crm"; } }
