using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class ShiftSwapRequest
    {
        [Key]
        public int ShiftSwapRequestId { get; set; }

        [Required]
        public int FromAssignmentId { get; set; }

        [ForeignKey("FromAssignmentId")]
        public StaffShiftAssignment FromAssignment { get; set; } = null!;

        [Required]
        public int ToAssignmentId { get; set; }

        [ForeignKey("ToAssignmentId")]
        public StaffShiftAssignment ToAssignment { get; set; } = null!;

        [Required]
        public int RequestedByUserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? ReviewNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
