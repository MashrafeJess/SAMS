using Business.DTO;
using Business.Interface;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassController(IClassService service) : ControllerBase
    {
        private readonly IClassService _service = service;
        [HttpPost("CreateClass")]
        public async Task<IActionResult> CreateClass(ClassDto dto)
        {
            var result = await _service.CreateClass(dto);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }

        [HttpGet("GetClassList")]
        public async Task<IActionResult> GetClassList()
        {
            var result = await _service.GetClassList();
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
        [HttpGet("GetClassListByShift")]
        public async Task<IActionResult> GetClassListByShift(long classId)
        {
            var result = await _service.GetClassListByShift(classId);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }

    }
}
