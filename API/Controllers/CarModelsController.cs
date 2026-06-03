using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CarModelsController : ControllerBase
    {
        private readonly ICarModelService _carModelService;

        public CarModelsController(ICarModelService carModelService)
        {
            _carModelService = carModelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCarModels()
        {
            var models = await _carModelService.GetActiveCarModelsAsync();
            return Ok(new { statusCode = 200, data = models });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCarModel([FromBody] CreateCarModelDTO request)
        {
            var result = await _carModelService.CreateCarModelAsync(request);
            if (!result)
                return BadRequest(new { statusCode = 400, message = "Thêm dòng xe thất bại!" });

            return Ok(new { statusCode = 201, message = "Thêm dòng xe thành công!" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCarModel(int id, [FromBody] UpdateCarModelDTO request)
        {
            var result = await _carModelService.UpdateCarModelAsync(id, request);
            if (!result)
                return NotFound(new { statusCode = 404, message = "Không tìm thấy dòng xe." });

            return Ok(new { statusCode = 200, message = "Cập nhật dòng xe thành công!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCarModel(int id)
        {
            var result = await _carModelService.DeleteCarModelAsync(id);
            if (!result)
                return NotFound(new { statusCode = 404, message = "Không tìm thấy dòng xe." });

            return Ok(new { statusCode = 200, message = "Đã ẩn dòng xe khỏi hệ thống." });
        }
    }
}
