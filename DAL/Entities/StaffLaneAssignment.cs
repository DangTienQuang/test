using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class StaffLaneAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public User Staff { get; set; } = null!;

        [Required]
        public int LaneId { get; set; }

        [ForeignKey("LaneId")]
        public Lane Lane { get; set; } = null!;

        [Required]
        public DateTime AssignedDate { get; set; }
    }
}
