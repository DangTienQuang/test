using DAL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public interface ILicensePlateService
    {
        // Single image (existing)
        Task<LicensePlateResult> DetectPlateAsync(byte[]? imageBytes);

        // Dual camera — new
        Task<DualPlateResult> DetectDualPlateAsync(
            byte[]? frontImageBytes,
            byte[]? backImageBytes);
    }

    public class LicensePlateResult
    {
        public bool Detected { get; set; }
        public string PlateText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public List<DAL.Entities.DetectionBox> Boxes { get; set; } = new();
    }
}
