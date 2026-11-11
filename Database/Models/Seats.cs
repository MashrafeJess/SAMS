using System.ComponentModel.DataAnnotations;

namespace Database.Models
{
    public class Seats
    {
        [Key]
        public long SeatId { get; set; }
        public long ClassId { get; set; }
        public long StudentId { get; set; }
        public long SeatNumber { get; set; }
    }
}
