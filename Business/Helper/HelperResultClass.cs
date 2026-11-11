using Database.AppContext;

namespace Business.Helper
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; } = null;

        public Result() { }

        public Result(bool isSuccess, string message, object? data)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
        }

        public static async Task<Result> CommitAsync(SamsDbContext dbContext, string Message, object? Data)
        {
            try
            {
                await dbContext.SaveChangesAsync();
                return new Result (true, Message, Data);
            }
            catch
            {
                throw new Exception("SaveChanges Failed, Please Debug");
            }
        }
    }
}
