using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database.Models;

namespace RadarrAPI.Database
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<UpdateEntity> UpdateEntities { get; set; }

        public DbSet<UpdateFileEntity> UpdateFileEntities { get; set; }

        public DbSet<TraktEntity> TraktEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UpdateEntity>(builder =>
            {
                builder.HasKey(k => k.UpdateEntityId);

                builder.HasMany(u => u.UpdateFiles)
                    .WithOne(u => u.Update)
                    .HasForeignKey(u => u.UpdateEntityId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
            
            modelBuilder.Entity<UpdateFileEntity>(builder =>
            {
                builder.HasKey(k => new {k.UpdateEntityId, k.OperatingSystem});
            });

            modelBuilder.Entity<TraktEntity>(builder =>
            {
                builder.HasKey(k => k.Id);
                builder.HasIndex(k => k.State);
            });
        }
    }
}
