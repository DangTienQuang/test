using AutoWashPro.BLL.Exceptions;
using BLL.DTOs;
using BLL.Helpers;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers.Ai
{
    [ApiController]
    [Route("api/v1/ai")]
    [EnableRateLimiting("AIChatPolicy")]
    [Authorize(Roles = "Customer")]
    public class AIChatbotController : ControllerBase
    {
        private readonly IAIChatbotService _aiService;

        public AIChatbotController(IAIChatbotService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AIChatRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                throw new BadRequestException("Message is required.");

            int userId = ClaimHelper.GetUserId(User);
            var result = await _aiService.ChatAsync(userId, request);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = result
            });
        }

        [HttpGet("recommendation")]
        public async Task<IActionResult> Recommendation()
        {
            int userId = ClaimHelper.GetUserId(User);
            var result = await _aiService.GetRecommendationAsync(userId);

            return Ok(new
            {
                statusCode = 200,
                message = "Success",
                data = new { recommendation = result }
            });
        }
    }
}