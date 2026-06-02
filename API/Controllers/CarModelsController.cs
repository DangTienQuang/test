using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using AutoWashPro.BLL.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CarModelsController : ControllerBase
    {
        private readonly AutoWashDbContext _context;

        public CarModelsController(AutoWashDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCarModels()
        {
            var models = await _context.CarModels
                .Where(c => c.IsActive)
                .OrderBy(c => c.Brand).ThenBy(c => c.Name)
                .Select(c => new CarModelDTO { Id = c.Id, Brand = c.Brand, Name = c.Name })
                .ToListAsync();
            return Ok(new { statusCode = 200, data = models });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCarModel([FromBody] CreateCarModelDTO request)
        {
            var newModel = new CarModel { Brand = request.Brand, Name = request.Name };
            _context.CarModels.Add(newModel);
            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 201, message = "Thêm dòng xe thành công!" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCarModel(int id, [FromBody] UpdateCarModelDTO request)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null)
                return NotFound(new { statusCode = 404, message = "Không tìm thấy dòng xe." });

            model.Brand = request.Brand;
            model.Name = request.Name;
            model.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Cập nhật dòng xe thành công!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCarModel(int id)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null)
                return NotFound(new { statusCode = 404, message = "Không tìm thấy dòng xe." });

            model.IsActive = false;

            await _context.SaveChangesAsync();
            return Ok(new { statusCode = 200, message = "Đã ẩn dòng xe khỏi hệ thống." });
        }
    }
}
