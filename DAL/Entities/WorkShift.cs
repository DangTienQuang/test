using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class WorkShift
    {
        [Key]
        public int WorkShiftId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string ShiftName { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<StaffShiftAssignment> Assignments { get; set; } = new List<StaffShiftAssignment>();
    }
}
