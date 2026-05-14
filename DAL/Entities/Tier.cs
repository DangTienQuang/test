using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class Tier
    {
        [Key]
        public int TierId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TierName { get; set; }

        public decimal PointMultiplier { get; set; }
        public int BookingWindowDays { get; set; }
        public int MaxActiveBookings { get; set; }
        public int RequiredPointsToUpgrade { get; set; }

        public ICollection<CustomerProfile> Customers { get; set; }
    }
}
