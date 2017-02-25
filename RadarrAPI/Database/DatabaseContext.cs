using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //// Define keys
            modelBuilder.Entity<UpdateEntity>().HasKey(k => k.UpdateEntityId);
            modelBuilder.Entity<UpdateFileEntity>().HasKey(k => new { k.UpdateEntityId, k.OperatingSystem });

            //// Define relations
            // An Update has many UpdateFiles
            modelBuilder.Entity<UpdateEntity>()
                .HasMany(u => u.UpdateFiles)
                .WithOne(u => u.Update)
                .HasForeignKey(u => u.UpdateEntityId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
