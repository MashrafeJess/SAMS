using System.ComponentModel;

namespace Database.Extensions
{
    public enum HoldStatus
    {
        [Description("PENDING")]
        Pending = 1,
        [Description("HELD")]
        Held = 2,
        [Description("RELEASED")]
        Released = 3,
        [Description("CONFIRMED")]
        Confirmed = 4
    }
}
