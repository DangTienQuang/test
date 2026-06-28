using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers.Admin
{
    [Route("api/v1/admin/vehicle-types")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class AdminVehicleTypeController : ControllerBase
    {
        private readonly IVehicleTypeService _typeService;

        public AdminVehicleTypeController(IVehicleTypeService typeService)
        {
            _typeService = typeService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleTypeDTO request)
        {
            var result = await _typeService.CreateAsync(request);
            return Created("", new { statusCode = 201, message = "Thêm loại xe thành công.", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateVehicleTypeDTO request)
        {
            await _typeService.UpdateAsync(id, request);
            return Ok(new { statusCode = 200, message = "Cập nhật loại xe thành công." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _typeService.DeleteAsync(id);
            return Ok(new { statusCode = 200, message = "Xóa loại xe thành công." });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _typeService.GetAllAsync();
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }
    }
}