<<<<<<< HEAD
using System.ComponentModel.DataAnnotations; using System.ComponentModel.DataAnnotations.Schema;
namespace Contact.API.Models { [Table("sale_headers")] public class SaleHeader { [Key] public int Id { get; set; } [Column("ClientId")] public int ClientId { get; set; } [Column("ServiceId")] public int ServiceId { get; set; } [Column("Price")] public decimal Price { get; set; } [Column("Date")] public DateTime Date { get; set; } [Column("Payment")] public string? Payment { get; set; } [Column("Status")] public string? Status { get; set; } [Column("Note")] public string? Note { get; set; } [Column("Total")] public decimal Total { get; set; } public List<SaleItem> Items { get; set; } = new(); } }
=======
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contact.API.Models
{
    [Table("sale_headers")]
    public class SaleHeader
    {
        [Key] public int Id { get; set; }
        [Column("ClientId")] public int ClientId { get; set; }
        [Column("ServiceId")] public int? ServiceId { get; set; }
        [Column("Price")] public decimal Price { get; set; }
        [Column("Date")] public DateTime Date { get; set; }
        [Column("Payment")] public string? Payment { get; set; }
        [Column("Status")] public string? Status { get; set; }
        [Column("Note")] public string? Note { get; set; }
        [Column("Total")] public decimal Total { get; set; }
        public List<SaleItem> Items { get; set; } = new();
    }
}
>>>>>>> f98bf5a (chore: cleanup gitignore, remove build artifacts)
