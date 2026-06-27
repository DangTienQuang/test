using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class SwapLaneByPhoneDTO
    {
        [Required]
        public string TargetStaffPhoneNumber { get; set; } = null!;

        public DateTime? Date { get; set; }
    }
}
