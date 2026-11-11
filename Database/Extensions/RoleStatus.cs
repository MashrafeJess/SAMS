using System.ComponentModel;

namespace Database.Extensions
{
    public enum RoleStatus
    {
        [Description("Admin")]
        Admin = 1,
        [Description("Student")]
        Common = 2
    }
}
