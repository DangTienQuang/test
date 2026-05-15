using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/tiers")]
    [ApiController]
    public class TierController : ControllerBase
    {
        private readonly ITierService _tierService;

        public TierController(ITierService tierService)
        {
            _tierService = tierService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTier([FromBody] CreateTierDTO request)
        {
            try
            {
                var result = await _tierService.CreateTierAsync(request);
                return Created("", new { statusCode = 201, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTiers()
        {
            try
            {
                var result = await _tierService.GetAllTiersAsync();
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}