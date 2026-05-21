using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoWashPro.API.Helpers;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public TransactionController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private int GetCurrentUserId()
        {
            return ClaimHelper.GetUserId(User);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var result = await _walletService.GetTransactionsAsync(GetCurrentUserId());
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpGet("points/history")]
        public async Task<IActionResult> GetPointsHistory()
        {
            try
            {
                var result = await _walletService.GetPointsHistoryAsync(GetCurrentUserId());
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
