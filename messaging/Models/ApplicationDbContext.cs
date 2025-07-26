using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace messaging.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<IncomingMessageItem> IncomingMessageItems { get; set; }
        public DbSet<IncomingMessageLog> IncomingMessageLogs { get; set; }
        public DbSet<OutgoingMessageItem> OutgoingMessageItems { get; set; }
        public DbSet<IJEItem> IJEItems { get; set; }

				// View-like non-entity model for stored procedures; does not have a real SQL table
				public DbSet<StatusResults> StatusResults { get; set; }
				
        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            AddTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).CreatedDate = DateTime.UtcNow;
                }
            }
        }

				protected override void OnModelCreating(ModelBuilder modelBuilder) {
						base.OnModelCreating(modelBuilder);
						modelBuilder.Entity<IncomingMessageItem>().HasKey(m => m.Id);
						modelBuilder.Entity<IncomingMessageLog>().HasKey(l => l.Id);
						modelBuilder.Entity<OutgoingMessageItem>().HasKey(m => m.Id);

						modelBuilder.Entity<StatusResults>(e => {
								e.Property(sr => sr.Source)
										.HasDefaultValue(""); //.HasColumnName("Source").IsRequired(false);
								e.Property(sr => sr.EventType)
										.HasDefaultValue(""); //ColumnName("EventType").IsRequired(false);
								e.Property(sr => sr.JurisdictionId)
										.HasDefaultValue(""); //.HasColumnName("JurisdictionId").IsRequired(false);
								e.HasNoKey();
						});
				}
    }
}
