using Business.DTO;
using Business.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IUserService userService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await userService.Login(dto);

            if (!result.IsSuccess || result.Data is not ClaimsPrincipal principal)
            {
                return Unauthorized(new { result.Message });
            }
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = dto.RememberMe });

            return Ok(new { result.Message });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { Message = "Logged out" });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me() => Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = User.Identity?.Name,
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });

        [HttpPost("AddStudent")]
        public async Task<IActionResult> AddStudent(UserDto dto)
        {
            var result = await userService.AddStudent(dto);
            if (!result.IsSuccess)
            {
                return BadRequest(new { result.Message });
            }
            return Ok(new { result.Message, result.Data });
        }
    }
}
