using Business.DTO;
using Business.Helper;
using Business.Interface;
using Database.AppContext;
using Database.Extensions;
using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Business.Service
{
    public class HoldService(SamsDbContext samsDbContext) : IHoldService
    {
        private readonly SamsDbContext _context = samsDbContext;

        /// <summary>How long a placed hold stays valid before it lapses.</summary>
        public static readonly TimeSpan HoldWindow = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Places a hold when <see cref="HoldDto.HoldId"/> is 0, otherwise confirms
        /// the hold with that id.
        /// </summary>
        public async Task<Result> CreateHold(HoldDto dto, CancellationToken ct = default)
        {
            var classDetails = await _context.Class
                .FirstOrDefaultAsync(x => x.ClassId == dto.ClassId, ct);

            if (classDetails is null)
            {
                return new Result(false, "Class not found", null);
            }

            return dto.HoldId > 0
                ? await ConfirmHold(dto, classDetails, ct)
                : await PlaceHold(dto, classDetails, ct);
        }

        private async Task<Result> PlaceHold(HoldDto dto, Class classDetails, CancellationToken ct)
        {
            // Without this the whole seat-accounting exercise is decorative:
            // nothing previously stopped a 40-seat class issuing a 41st hold.
            var available = classDetails.SeatNumber - (classDetails.HoldNumber + classDetails.ConfirmedSeats);
            if (available <= 0)
            {
                return new Result(false, "No seats left in this class", null);
            }

            var now = DateTime.UtcNow;
            var hold = new Holds
            {
                ClassId = dto.ClassId,
                StudentId = dto.StudentId,
                HoldStatusId = (long)HoldStatus.Held,
                HoldStatusName = HoldStatus.Held.ToString(),
                CreatedDate = now,
                ExpiresAt = now.Add(HoldWindow)
            };

            await _context.Holds.AddAsync(hold, ct);
            classDetails.HoldNumber += 1;

            return await Result.CommitAsync(_context, "Seat held successfully", hold);
        }

        private async Task<Result> ConfirmHold(HoldDto dto, Class classDetails, CancellationToken ct)
        {
            var hold = await _context.Holds.FirstOrDefaultAsync(x => x.HoldId == dto.HoldId, ct);
            if (hold is null)
            {
                return new Result(false, "Hold not found", null);
            }
            if (hold.HoldStatusId == (long)HoldStatus.Confirmed)
            {
                return new Result(true, "Seat is already confirmed", hold);
            }
            if (hold.HoldStatusId == (long)HoldStatus.Released)
            {
                return new Result(false, "That hold has already been released", null);
            }

            // Both sides UTC. Comparing two DateTime values compares ticks and
            // ignores Kind entirely, so mixing DateTime.Now (local) with an
            // ExpiresAt written from UtcNow made every hold look expired by the
            // size of the machine's UTC offset.
            if (DateTime.UtcNow > hold.ExpiresAt)
            {
                // Lapse it here and now rather than leaving the seat stranded
                // until the sweeper next runs.
                ReleaseInMemory(hold, classDetails);
                await _context.SaveChangesAsync(ct);
                return new Result(false, "Hold has expired", null);
            }

            hold.HoldStatusId = (long)HoldStatus.Confirmed;
            hold.HoldStatusName = HoldStatus.Confirmed.ToString();

            // The seat moves from the held bucket to the confirmed bucket; the
            // total occupied count is unchanged.
            classDetails.ConfirmedSeats += 1;
            if (classDetails.HoldNumber > 0)
            {
                classDetails.HoldNumber -= 1;
            }

            return await Result.CommitAsync(_context, "Seat confirmed successfully", hold);
        }

        public async Task<Result> GetHoldById(long HoldId, CancellationToken ct = default)
        {
            var hold = await _context.Holds.FirstOrDefaultAsync(x => x.HoldId == HoldId, ct);
            return hold is null
                ? new Result(false, "Hold not found", null)
                : new Result(true, "Hold fetched successfully", hold);
        }

        /// <summary>
        /// Permanently removes a hold and returns its seat to the pool.
        /// </summary>
        public async Task<Result> DeleteHold(long HoldId, CancellationToken ct = default)
        {
            var hold = await _context.Holds.FirstOrDefaultAsync(x => x.HoldId == HoldId, ct);
            if (hold is null)
            {
                return new Result(false, "Hold not found", null);
            }

            var classDetails = await _context.Class
                .FirstOrDefaultAsync(x => x.ClassId == hold.ClassId, ct);

            if (classDetails is not null)
            {
                // Give the seat back to whichever bucket this hold occupied.
                // A Released hold already gave its seat back, so it owes nothing.
                if (hold.HoldStatusId is (long)HoldStatus.Held or (long)HoldStatus.Pending)
                {
                    if (classDetails.HoldNumber > 0)
                    {
                        classDetails.HoldNumber -= 1;
                    }
                }
                else if (hold.HoldStatusId == (long)HoldStatus.Confirmed)
                {
                    if (classDetails.ConfirmedSeats > 0)
                    {
                        classDetails.ConfirmedSeats -= 1;
                    }
                }
            }

            _context.Holds.Remove(hold);

            // Remove() only marks the entry Deleted on the change tracker.
            // Nothing reaches the database until SaveChanges -- which is exactly
            // what this method used to skip, so "deleted successfully" was a lie.
            return await Result.CommitAsync(_context, "Hold deleted successfully", hold);
        }

        public async Task<Result> GetHoldListByStudentId(long StudentId, CancellationToken ct = default)
        {
            var holdList = await _context.Holds
                .Where(x => x.StudentId == StudentId)
                .OrderByDescending(x => x.HoldId)
                .ToListAsync(ct);

            return new Result(true, "Hold list fetched successfully", holdList);
        }

        /// <summary>
        /// Marks every lapsed hold as Released and hands its seat back to the
        /// class. Safe to call repeatedly: a hold is only ever released once,
        /// because the filter excludes anything already Released or Confirmed.
        /// </summary>
        public async Task<Result> ReleaseExpiredHolds(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var lapsed = await _context.Holds
                .Where(h => (h.HoldStatusId == (long)HoldStatus.Held
                             || h.HoldStatusId == (long)HoldStatus.Pending)
                            && h.ExpiresAt <= now)
                .ToListAsync(ct);

            if (lapsed.Count == 0)
            {
                return new Result(true, "No expired holds", 0);
            }

            // Load the affected classes in one query rather than one per hold.
            var classIds = lapsed.Select(h => h.ClassId).Distinct().ToList();
            var classes = await _context.Class
                .Where(c => classIds.Contains(c.ClassId))
                .ToDictionaryAsync(c => c.ClassId, ct);

            foreach (var hold in lapsed)
            {
                classes.TryGetValue(hold.ClassId, out var classDetails);
                ReleaseInMemory(hold, classDetails);
            }

            await _context.SaveChangesAsync(ct);

            return new Result(true, $"Released {lapsed.Count} expired hold(s)", lapsed.Count);
        }

        /// <summary>
        /// Flips a hold to Released and decrements the class's held counter.
        /// Does not save -- the caller decides the transaction boundary.
        /// </summary>
        private static void ReleaseInMemory(Holds hold, Class? classDetails)
        {
            hold.HoldStatusId = (long)HoldStatus.Released;
            hold.HoldStatusName = HoldStatus.Released.ToString();

            // Clamped at zero so a counter that has already drifted cannot be
            // driven negative and make availableSeats exceed the room size.
            if (classDetails is not null && classDetails.HoldNumber > 0)
            {
                classDetails.HoldNumber -= 1;
            }
        }
    }
}
