using Microsoft.EntityFrameworkCore;

namespace NVSSMessaging.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FHIRMessageItem> FHIRMessageItems { get; set; }
        public DbSet<IJEItem> IJEItems { get; set; }
    }
}
