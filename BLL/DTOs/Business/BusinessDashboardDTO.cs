using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class BusinessDashboardDTO
    {
        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int TodayWashes { get; set; }
        public int MonthWashes { get; set; }
        public decimal MonthCost { get; set; }
    }
}
