using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class ManagerProfile
    {
        [Key]
        public int ManagerProfileId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        public DateTime? HiredDate { get; set; }
    }
}
