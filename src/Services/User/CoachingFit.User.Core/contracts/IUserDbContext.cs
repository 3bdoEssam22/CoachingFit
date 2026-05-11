
using CoachingFit.User.Core.Entities;

namespace CoachingFit.User.Core.Contracts
{
    public interface IUserDbContext
    {
        IQueryable<CoachProfile> CoachProfiles { get; }
        IQueryable<TraineeProfile> TraineeProfiles { get; }
        IQueryable<CoachCertificate> CoachCertificates { get; }

        Task<CoachProfile?> FindCoachProfileAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TraineeProfile?> FindTraineeProfileAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CoachCertificate?> FindCoachCertificateAsync(Guid id, CancellationToken cancellationToken = default);

        Task AddCoachProfileAsync(CoachProfile profile, CancellationToken cancellationToken = default);
        Task AddTraineeProfileAsync(TraineeProfile profile, CancellationToken cancellationToken = default);
        Task AddCoachCertificateAsync(CoachCertificate certificate, CancellationToken cancellationToken = default);

        void UpdateCoachProfile(CoachProfile profile);
        void UpdateTraineeProfile(TraineeProfile profile);
        void UpdateCoachCertificate(CoachCertificate certificate);
        void RemoveCoachCertificate(CoachCertificate certificate);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
