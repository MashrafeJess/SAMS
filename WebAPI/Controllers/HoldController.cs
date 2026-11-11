using Business.DTO;
using Business.Interface;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HoldController(IHoldService holdService) : ControllerBase
    {
        private readonly IHoldService _holdService = holdService;
        [HttpPost("CreateHold")]
        public async Task<IActionResult> CreateHold(HoldDto dto)
        {
            var result = await _holdService.CreateHold(dto);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
        [HttpGet("GetHoldById")]
        public async Task<IActionResult> GetHoldById(long HoldId)
        {
            var result = await _holdService.GetHoldById(HoldId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
        [HttpDelete("DeleteHold")]
        public async Task<IActionResult> DeleteHold(long HoldId)
        {
            var result = await _holdService.DeleteHold(HoldId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
        [HttpGet("me/holds")]
        public async Task<IActionResult> GetHoldListByStudentId(long StudentId)
        {
            var result = await _holdService.GetHoldListByStudentId(StudentId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
    }
}
