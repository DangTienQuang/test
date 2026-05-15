using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/services")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly IOperationService _operationService;

        public ServicesController(IOperationService operationService)
        {
            _operationService = operationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var result = await _operationService.GetServicesAsync();
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceDTO request)
        {
            try
            {
                var result = await _operationService.CreateServiceAsync(request);
                return Created("", new { statusCode = 201, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }

}