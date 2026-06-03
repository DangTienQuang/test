using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class AssignBookingDetailDTO
    {
        [Required]
        public int BookingDetailId { get; set; }

        [Required]
        public int LaneId { get; set; }

        [Required]
        public int StaffId { get; set; }
    }
}
