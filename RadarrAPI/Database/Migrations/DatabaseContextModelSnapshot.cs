using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace RadarrAPI.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdateEntity", b =>
                {
                    b.Property<int>("UpdateEntityId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Branch");

                    b.Property<string>("FixedStr")
                        .HasColumnName("Fixed");

                    b.Property<string>("NewStr")
                        .HasColumnName("New");

                    b.Property<DateTime>("ReleaseDate");

                    b.Property<string>("Version");

                    b.HasKey("UpdateEntityId");

                    b.ToTable("Updates");
                });

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdateFileEntity", b =>
                {
                    b.Property<int>("UpdateEntityId");

                    b.Property<int>("OperatingSystem");

                    b.Property<string>("Filename");

                    b.Property<string>("Hash");

                    b.Property<string>("Url");

                    b.HasKey("UpdateEntityId", "OperatingSystem");

                    b.ToTable("UpdateFiles");
                });

            modelBuilder.Entity("RadarrAPI.Database.Models.UpdateFileEntity", b =>
                {
                    b.HasOne("RadarrAPI.Database.Models.UpdateEntity", "Update")
                        .WithMany("UpdateFiles")
                        .HasForeignKey("UpdateEntityId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
