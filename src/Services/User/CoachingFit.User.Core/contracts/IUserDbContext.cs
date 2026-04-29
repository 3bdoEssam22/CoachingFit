
using CoachingFit.User.Core.Entities;

namespace CoachingFit.User.Core.Contracts
{
    public interface IUserDbContext
    {
        IQueryable<CoachProfile> CoachProfiles { get; }
        IQueryable<TraineeProfile> TraineeProfiles { get; }

        Task<CoachProfile?> FindCoachProfileAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TraineeProfile?> FindTraineeProfileAsync(Guid id, CancellationToken cancellationToken = default);

        Task AddCoachProfileAsync(CoachProfile profile, CancellationToken cancellationToken = default);
        Task AddTraineeProfileAsync(TraineeProfile profile, CancellationToken cancellationToken = default);

        void UpdateCoachProfile(CoachProfile profile);
        void UpdateTraineeProfile(TraineeProfile profile);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
