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
            var userId = ClaimHelper.GetUserId(User);
            var result = await _walletService.GetWalletInfoAsync(userId);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [Authorize]
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDTO request)
        {
            var userId = ClaimHelper.GetUserId(User);
            var result = await _walletService.CreateTopUpLinkAsync(userId, request);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("top-up/callback")]
        public async Task<IActionResult> PayOSWebhook([FromBody] WebhookTopUpDTO webhookData)
        {
            await _walletService.ProcessPaymentWebhookAsync(webhookData);
            return Ok(new { statusCode = 200, message = "Success" });
        }
    }
}
