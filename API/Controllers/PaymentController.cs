using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public PaymentController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost("payos/webhook")]
        public async Task<IActionResult> PayOsWebhook([FromBody] WebhookTopUpDTO webhookData)
        {
            await _walletService.ProcessPayOsWebhookAsync(webhookData);
            return Ok(new { statusCode = 200, message = "Success" });
        }
    }
}
