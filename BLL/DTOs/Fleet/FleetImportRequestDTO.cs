using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Fleet
{
    public class FleetImportRequestDTO
    {
        public IFormFile File { get; set; } = null;
    }
}
