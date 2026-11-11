using Database.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Database.Models
{
    public class Users
    {
        [Key]
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public long RoleId { get; set; } = (long)RoleStatus.Common;
        public string RoleName { get; set; } = RoleStatus.Common.ToString();
    }
}
