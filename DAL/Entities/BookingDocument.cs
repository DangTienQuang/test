using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWashPro.DAL.Entities
{
    public class BookingDocument
    {
        public int BookingDocumentId { get; set; }

        public int BookingId { get; set; }

        public string DocumentType { get; set; } = null!;

        public string FileUrl { get; set; } = null!;

        public DateTime GeneratedAt { get; set; }

        public Booking Booking { get; set; } = null!;
    }

}
