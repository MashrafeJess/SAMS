using Business.DTO;
using Business.Helper;

namespace Business.Interface
{
    public interface IUserService
    {
        Task<Result> AddStudent(UserDto dto);
        Task<Result> Login(LoginDto dto);
    }
}
