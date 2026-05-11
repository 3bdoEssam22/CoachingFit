using CoachingFit.User.Core.Contracts;
using CoachingFit.User.Core.Entities;
using CoachingFit.User.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.User.Infrastructure.Data.Context
{
    public class UserDbContext(DbContextOptions<UserDbContext> options)
            : DbContext(options), IUserDbContext
    {
        public DbSet<CoachProfile> CoachProfilesSet { get; set; }
        public DbSet<TraineeProfile> TraineeProfilesSet { get; set; }
        public DbSet<CoachCertificate> CoachCertificatesSet { get; set; }

        public IQueryable<CoachProfile> CoachProfiles => CoachProfilesSet;
        public IQueryable<TraineeProfile> TraineeProfiles => TraineeProfilesSet;
        public IQueryable<CoachCertificate> CoachCertificates => CoachCertificatesSet;

        public async Task<CoachProfile?> FindCoachProfileAsync(Guid id, CancellationToken cancellationToken = default)
            => await CoachProfilesSet.FindAsync([id], cancellationToken);

        public async Task<TraineeProfile?> FindTraineeProfileAsync(Guid id, CancellationToken cancellationToken = default)
            => await TraineeProfilesSet.FindAsync([id], cancellationToken);

        public async Task<CoachCertificate?> FindCoachCertificateAsync(Guid id, CancellationToken cancellationToken = default)
            => await CoachCertificatesSet.FindAsync([id], cancellationToken);

        public async Task AddCoachProfileAsync(CoachProfile profile, CancellationToken cancellationToken = default)
            => await CoachProfilesSet.AddAsync(profile, cancellationToken);

        public async Task AddTraineeProfileAsync(TraineeProfile profile, CancellationToken cancellationToken = default)
            => await TraineeProfilesSet.AddAsync(profile, cancellationToken);

        public async Task AddCoachCertificateAsync(CoachCertificate certificate, CancellationToken cancellationToken = default)
            => await CoachCertificatesSet.AddAsync(certificate, cancellationToken);

        public void UpdateCoachProfile(CoachProfile profile)
            => CoachProfilesSet.Update(profile);

        public void UpdateTraineeProfile(TraineeProfile profile)
            => TraineeProfilesSet.Update(profile);

        public void UpdateCoachCertificate(CoachCertificate certificate)
            => CoachCertificatesSet.Update(certificate);

        public void RemoveCoachCertificate(CoachCertificate certificate)
            => CoachCertificatesSet.Remove(certificate);

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
                entity.Property(e => e.Gender).HasConversion<int>();
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
                entity.Property(e => e.Gender).HasConversion<int>();
                entity.Property(e => e.FitnessLevel).HasConversion<int>();
            });

            builder.Entity<CoachCertificate>(entity =>
            {
                entity.ToTable("CoachCertificates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IssuingOrganization).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FileUrl).IsRequired();
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileType).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(CertificateStatus.Pending);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
                entity.HasOne(e => e.CoachProfile)
                      .WithMany()
                      .HasForeignKey(e => e.CoachProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CoachProfileId, e.Status });
            });
        }
    }
}
