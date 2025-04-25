using Microsoft.EntityFrameworkCore;

namespace status_api.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { }

        // Only define getters because this database is read-only
        public DbSet<IncomingMessageItem> IncomingMessageItems { get; }
        public DbSet<OutgoingMessageItem> OutgoingMessageItems { get; }
        public DbSet<IJEItem> IJEItems { get; }

    }
}
