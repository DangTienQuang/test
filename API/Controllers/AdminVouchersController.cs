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
            try
            {
                var result = await _voucherService.GetAllVouchersAsync();
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrUpdateVoucherDTO request)
        {
            try
            {
                var result = await _voucherService.CreateVoucherAsync(request);
                return Created("", new { statusCode = 201, message = "Tạo voucher thành công.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateOrUpdateVoucherDTO request)
        {
            try
            {
                var result = await _voucherService.UpdateVoucherAsync(id, request);
                return Ok(new { statusCode = 200, message = "Cập nhật voucher thành công.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _voucherService.DeleteVoucherAsync(id);
                return Ok(new { statusCode = 200, message = "Xóa voucher thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
