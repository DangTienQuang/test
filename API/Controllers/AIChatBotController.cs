using BLL.DTOs;
using BLL.Helpers;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/ai")]
    [EnableRateLimiting("AIChatPolicy")]
    [Authorize(Roles = "Customer")]
    public class AIChatbotController : ControllerBase
    {
        private readonly IAIChatbotService _aiService;

        public AIChatbotController(
            IAIChatbotService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat(
            [FromBody] AIChatRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Message is required."
                    });
                }

                int userId = ClaimHelper.GetUserId(User);

                var result = await _aiService
                    .ChatAsync(userId, request);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Success",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
        }

        [HttpGet("recommendation")]
        public async Task<IActionResult> Recommendation()
        {
            try
            {
                int userId = ClaimHelper.GetUserId(User);

                var result = await _aiService
                    .GetRecommendationAsync(userId);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Success",
                    data = new
                    {
                        recommendation = result
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
        }
    }
}