using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Fleet
{
    public class FleetImportErrorDTO
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = null;
    }
}
