using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers.Admin
{
    [ApiController]
    [Route("api/v1/admin/lanes")]
    [Authorize(Roles = "Admin")]
    public class AdminLanesController : ControllerBase
    {
        private readonly ILaneService _laneService;
        private readonly IFleetService _fleetService;

        public AdminLanesController(ILaneService laneService, IFleetService fleetService)
        {
            _laneService = laneService;
            _fleetService = fleetService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLanes([FromQuery] int? branchId)
        {
            var lanes = await _laneService.GetAllLanesAsync(branchId);
            return Ok(lanes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLane(int id)
        {
            var lane = await _laneService.GetLaneByIdAsync(id);
            return Ok(lane);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLane([FromBody] CreateLaneDTO dto)
        {
            var lane = await _laneService.CreateLaneAsync(dto);
            return CreatedAtAction(nameof(GetLane), new { id = lane.LaneId }, lane);
        }

        [HttpPost("business")]
        public async Task<IActionResult> CreateBusinessLane([FromBody] CreateBusinessLaneDTO dto)
        {
            var lane = await _fleetService.CreateBusinessLaneAsync(dto);
            return CreatedAtAction(nameof(GetLane), new { id = lane.LaneId }, lane);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLane(int id, [FromBody] UpdateLaneDTO dto)
        {
            var lane = await _laneService.UpdateLaneAsync(id, dto);
            return Ok(lane);
        }
    }
}
