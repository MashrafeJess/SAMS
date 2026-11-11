using Business.DTO;
using Business.Helper;

namespace Business.Interface
{
    public interface IHoldService
    {
        Task<Result> CreateHold(HoldDto dto, CancellationToken ct = default);
        public Task<Result> GetHoldById(long HoldId, CancellationToken ct = default);
        public Task<Result> DeleteHold(long HoldId, CancellationToken ct = default);
        public Task<Result> GetHoldListByStudentId(long StudentId, CancellationToken ct = default);

        /// <summary>
        /// Releases every hold whose ExpiresAt has passed, returning their seats.
        /// Called on a timer by the hold-expiry background service.
        /// </summary>
        Task<Result> ReleaseExpiredHolds(CancellationToken ct = default);
    }
}
