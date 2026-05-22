using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class ServiceInventoryRequirement
    {
        [Key]
        public int RequirementId { get; set; }

        [ForeignKey("Service")]
        public int ServiceId { get; set; }
        public Service Service { get; set; }

        [ForeignKey("InventoryItem")]
        public int ItemId { get; set; }
        public InventoryItem InventoryItem { get; set; }

        public decimal QuantityRequired { get; set; }
    }
}
