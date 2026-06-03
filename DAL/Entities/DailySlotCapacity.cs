using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class DailySlotCapacity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SlotId { get; set; }

        [ForeignKey("SlotId")]
        public TimeSlot TimeSlot { get; set; } = null!;

        [Required]
        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch Branch { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int BookedWeight { get; set; } = 0;

        [Timestamp]
        public DateTime? RowVersion { get; set; }
    }
}
