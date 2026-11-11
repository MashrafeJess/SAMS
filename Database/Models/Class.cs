using System.ComponentModel.DataAnnotations;

namespace Database.Models
{
    public class Class
    {
        [Key]
        public long ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public long SeatNumber { get; set; }
        public long HoldNumber { get; set; }
        public long ConfirmedSeats { get; set; }
        public long ShiftId { get; set; }
    }
}
