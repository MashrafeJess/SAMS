using Business.DTO;
using Business.Helper;
using Business.Interface;
using Database.AppContext;
using Microsoft.EntityFrameworkCore;

namespace Business.Service
{
    public class ClassService(SamsDbContext samsDbContext) : IClassService
    {
        private readonly SamsDbContext _context = samsDbContext;

        /// <summary>
        /// Creates a class, or updates it when the DTO carries an existing ClassId.
        /// </summary>
        public async Task<Result> CreateClass(ClassDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ClassName))
            {
                return new Result(false, "Class name is required", null);
            }
            if (dto.TotalSeats <= 0)
            {
                return new Result(false, "A class needs at least one seat", null);
            }

            // Only look for an existing row when the caller actually named one.
            // ClassId is long?, so on a create it is null and this query would
            // match nothing anyway -- skipping it saves a round trip.
            var existClass = dto.ClassId is > 0
                ? await _context.Class.FirstOrDefaultAsync(x => x.ClassId == dto.ClassId)
                : null;

            if (existClass is null)
            {
                var newClass = new Database.Models.Class
                {
                    ClassName = dto.ClassName,
                    SeatNumber = dto.TotalSeats,
                    ShiftId = dto.ShiftId,
                    HoldNumber = 0,
                    ConfirmedSeats = 0
                };
                await _context.Class.AddAsync(newClass);

                return await Result.CommitAsync(_context, "Class created successfully", newClass);
            }

            var allocated = existClass.HoldNumber + existClass.ConfirmedSeats;
            if (dto.TotalSeats < allocated)
            {
                return new Result(
                    false,
                    $"Cannot reduce to {dto.TotalSeats} seats: {allocated} are already held or confirmed",
                    null);
            }
            existClass.ClassName = dto.ClassName;
            existClass.SeatNumber = dto.TotalSeats;
            existClass.ShiftId = dto.ShiftId;

            return await Result.CommitAsync(_context, "Class updated successfully", existClass);
        }
        public async Task<Result> GetClassList()
        {
            try
            {
                var classList = await(from c in _context.Class
                                      join s in _context.Shift on c.ShiftId equals s.ShiftId into h
                                      from s in h.DefaultIfEmpty()
                                      select new
                                      {
                                          id = c.ClassId,
                                          name = c.ClassName + " - " + s.ShiftName,
                                          totalSeats = c.SeatNumber,
                                          heldSeats = c.HoldNumber,
                                          confirmedSeats = c.ConfirmedSeats,
                                          availableSeats = c.SeatNumber - (c.HoldNumber + c.ConfirmedSeats)
                                      }).ToListAsync();
                return new Result
                {
                    IsSuccess = true,
                    Message = "Class list fetched successfully",
                    Data = classList
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch Class list" + ex);
            }
        }
        public async Task<Result> GetClassListByShift(long classId)
        {
            try
            {
                var classList = await (from c in _context.Class
                                       join s in _context.Shift on c.ShiftId equals s.ShiftId into h
                                       from s in h.DefaultIfEmpty()
                                       where c.ClassId == classId
                                       select new
                                       {
                                           id = c.ClassId,
                                           name = c.ClassName + " - " + s.ShiftName,
                                           totalSeats = c.SeatNumber,
                                           heldSeats = c.HoldNumber,
                                           confirmedSeats = c.ConfirmedSeats,
                                           availableSeats = c.SeatNumber - (c.HoldNumber + c.ConfirmedSeats)
                                       }).ToListAsync();
                return new Result
                {
                    IsSuccess = true,
                    Message = "Class list fetched successfully",
                    Data = classList
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch Class list" + ex);
            }
        }
    }
}
