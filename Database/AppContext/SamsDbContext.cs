using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.AppContext
{
    public class SamsDbContext(DbContextOptions<SamsDbContext> options) : DbContext(options)
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<Class> Class { get; set; }
        public DbSet<Holds> Holds { get; set; }
        public DbSet<Seats> Seats { get; set; }
        public DbSet<Shifts> Shift { get; set; }
    }
}
