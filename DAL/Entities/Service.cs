using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.DAL.Entities
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; }

        public decimal BasePrice { get; set; }
        public int DurationMinutes { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}