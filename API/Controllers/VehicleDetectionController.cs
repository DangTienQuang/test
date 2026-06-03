using AutoWashPro.BLL.Exceptions;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleDetectionController : ControllerBase
    {
        private readonly ILicensePlateService _plateService;

        public VehicleDetectionController(ILicensePlateService plateService)
        {
            _plateService = plateService;
        }

        [HttpPost("detect-plate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> DetectPlate(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new BadRequestException("No image provided.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            var result = await _plateService.DetectPlateAsync(ms.ToArray());

            if (!result.Detected)
                throw new NotFoundException("No license plate detected.");

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = new
                {
                    plateText = result.PlateText,
                    confidence = result.Confidence
                }
            });
        }

        [HttpPost("detect-dual-plate")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> DetectDualPlate(
            IFormFile? frontImage,
            IFormFile? backImage)
        {
            if (frontImage == null && backImage == null)
                throw new BadRequestException("At least one image required.");

            byte[]? frontBytes = null;
            byte[]? backBytes = null;

            if (frontImage != null)
            {
                using var ms = new MemoryStream();
                await frontImage.CopyToAsync(ms);
                frontBytes = ms.ToArray();
            }

            if (backImage != null)
            {
                using var ms = new MemoryStream();
                await backImage.CopyToAsync(ms);
                backBytes = ms.ToArray();
            }

            var result = await _plateService.DetectDualPlateAsync(frontBytes, backBytes);

            if (!result.Detected)
                throw new NotFoundException("No license plate detected.");

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = new
                {
                    plateText = result.FinalPlateText,
                    confirmedBy = result.ConfirmedBy,
                    front = result.Front == null ? null : (object)new
                    {
                        detected = result.Front.Detected,
                        plateText = result.Front.PlateText,
                        confidence = result.Front.Confidence,
                        plateType = result.Front.PlateType
                    },
                    back = result.Back == null ? null : (object)new
                    {
                        detected = result.Back.Detected,
                        plateText = result.Back.PlateText,
                        confidence = result.Back.Confidence,
                        plateType = result.Back.PlateType
                    }
                }
            });
        }
    }
}