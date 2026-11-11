using Business.DTO;
using Business.Helper;
using Business.Interface;
using Database.AppContext;
using Database.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Business.Service
{
    public class UserService(SamsDbContext dbContext) : IUserService
    {
        /// <summary>
        /// Must match CookieAuthenticationDefaults.AuthenticationScheme in the API layer.
        /// Kept as a literal so this project stays free of ASP.NET Core dependencies.
        /// </summary>
        public const string AuthenticationScheme = "Cookies";

        private readonly SamsDbContext _context = dbContext;
        private readonly PasswordHasher<Users> _passwordHasher = new();

        /// <summary>
        /// A valid hash to verify against when the email doesn't exist, so a wrong
        /// email and a wrong password cost the same time. Prevents user enumeration.
        /// </summary>
        private static readonly string DummyHash =
            new PasswordHasher<Users>().HashPassword(new Users(), "not-a-real-password");

        public async Task<Result> AddStudent(UserDto dto)
        {
            try
            {
                var emailTaken = await _context.Users.AnyAsync(x => x.Email == dto.Email);
                if (emailTaken)
                {
                    return new Result(false, $"A student with the email {dto.Email} already exists", null);
                }
                var student = new Users
                {
                    UserName = dto.UserName,
                    Email = dto.Email
                };
                student.Password = _passwordHasher.HashPassword(student, dto.Password);

                _context.Users.Add(student);

                return await Result.CommitAsync( _context, "Operation Successful",
                    new { student.UserId, student.UserName, student.Email, student.RoleName });
            }
            catch (Exception ex)
            {
                return new Result(false, ex.Message, null);
            }
        }

        public async Task<Result> Login(LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == dto.Email);

                var verification = _passwordHasher.VerifyHashedPassword(
                    user ?? new Users(),
                    user?.Password ?? DummyHash,
                    dto.Password);

                if (user is null || verification == PasswordVerificationResult.Failed)
                {
                    return new Result(false, "Invalid email or password", null);
                }

                if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.Password = _passwordHasher.HashPassword(user, dto.Password);
                    await _context.SaveChangesAsync();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new(ClaimTypes.Name, user.UserName),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.RoleName)
                };

                var identity = new ClaimsIdentity(claims, AuthenticationScheme);

                return new Result(true, "Login Successful", new ClaimsPrincipal(identity));
            }
            catch (Exception ex)
            {
                return new Result(false, ex.Message, null);
            }
        }
    }
}
