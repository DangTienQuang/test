using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        public int UserId { get; set; }

        public string Action { get; set; }

        public string EntityName { get; set; }

        public string EntityId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
