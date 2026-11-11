using System;
using System.Collections.Generic;
using System.Text;

namespace Business.DTO
{
    public class ClassDto
    {
        public long? ClassId { get; set; }
        public string ClassName { get; set; }
        public long ShiftId { get; set; }
        public long TotalSeats { get; set; }
        public long HoldNumber { get; set; }
        public long ConfirmedSeat { get; set; }
    }
}
