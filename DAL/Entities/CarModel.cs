using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class CarModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } // Hãng xe (VD: Toyota, Mazda)

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Tên dòng xe (VD: Vios, CX-5)

        public bool IsActive { get; set; } = true;
    }
}
