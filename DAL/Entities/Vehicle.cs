using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Vehicle
    {
        [Key]
        [MaxLength(20)]
        public string LicensePlate { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(20)]
        public string VehicleType { get; set; }
    }
}
