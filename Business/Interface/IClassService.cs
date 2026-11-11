using Business.DTO;
using Business.Helper;

namespace Business.Interface
{
    public interface IClassService
    {
        public Task<Result> CreateClass(ClassDto dto);
        public Task<Result> GetClassList();
        public Task<Result> GetClassListByShift(long classId);
    }
}