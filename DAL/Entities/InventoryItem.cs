using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class InventoryItem
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; }

        public decimal Quantity { get; set; }

        public string Unit { get; set; }

        public decimal CostPerUnit { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
