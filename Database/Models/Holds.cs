using Database.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Database.Models
{
    public class Holds
    {
        [Key]
        public long HoldId { get; set; }
        public long ClassId { get; set; }
        public long StudentId { get; set; }
        public long HoldStatusId { get; set; } = (long)HoldStatus.Pending;
        public string HoldStatusName { get; set; } = HoldStatus.Pending.ToString();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddMinutes(5);
    }
}
