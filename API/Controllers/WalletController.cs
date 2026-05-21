using System;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AutoWashPro.API.Helpers;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/wallets")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet()
        {
            try
            {
                var userId = ClaimHelper.GetUserId(User);
                var result = await _walletService.GetWalletInfoAsync(userId);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDTO request)
        {
            try
            {
                var userId = ClaimHelper.GetUserId(User);
                var result = await _walletService.CreateTopUpLinkAsync(userId, request);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPost("top-up/callback")]
        public async Task<IActionResult> PayOSWebhook([FromBody] WebhookTopUpDTO webhookData)
        {
            try
            {
                await _walletService.ProcessPaymentWebhookAsync(webhookData);
                return Ok(new { statusCode = 200, message = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
