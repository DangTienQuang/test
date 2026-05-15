using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class CustomerProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [ForeignKey("Tier")]
        public int TierId { get; set; }
        public Tier Tier { get; set; }

        public double ChurnScore { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }
}
