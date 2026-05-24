using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/vouchers")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminVouchersController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public AdminVouchersController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _voucherService.GetAllVouchersAsync();
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrUpdateVoucherDTO request)
        {
            var result = await _voucherService.CreateVoucherAsync(request);
            return Created("", new { statusCode = 201, message = "Tạo voucher thành công.", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateOrUpdateVoucherDTO request)
        {
            var result = await _voucherService.UpdateVoucherAsync(id, request);
            return Ok(new { statusCode = 200, message = "Cập nhật voucher thành công.", data = result });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _voucherService.DeleteVoucherAsync(id);
            return Ok(new { statusCode = 200, message = "Xóa voucher thành công." });
        }
    }
}
