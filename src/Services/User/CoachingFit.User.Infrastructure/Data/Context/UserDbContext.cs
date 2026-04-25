using CoachingFit.User.Core.Contracts;
using CoachingFit.User.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.User.Infrastructure.Data.Context
{
    public class UserDbContext(DbContextOptions<UserDbContext> options)
            : DbContext(options), IUserDbContext
    {
        public DbSet<CoachProfile> CoachProfiles { get; set; }
        public DbSet<TraineeProfile> TraineeProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema("user");

            builder.Entity<CoachProfile>(entity =>
            {
                entity.ToTable("CoachProfiles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.Bio).IsRequired().HasMaxLength(1000);
            });

            builder.Entity<TraineeProfile>(entity =>
            {
                entity.ToTable("TraineeProfiles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.Goals).IsRequired().HasMaxLength(500);
                entity.Property(e => e.MedicalNotes).HasMaxLength(500);
            });
        }
    }
}
