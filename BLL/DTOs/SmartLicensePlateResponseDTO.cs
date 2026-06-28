using System;
using System.Collections.Generic;

namespace AutoWashPro.BLL.DTOs
{
    public class SmartLicensePlateResponseDTO
    {
        public string CustomerType { get; set; } = "WalkIn";
        public object? Data { get; set; }
    }
}
