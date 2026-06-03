using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class UpdateBookingStatusDTO
    {
        [Required]
        [RegularExpression("^(Processing|Completed)$", ErrorMessage = "Status must be Processing or Completed.")]
        public string Status { get; set; } = null!;
    }
}
