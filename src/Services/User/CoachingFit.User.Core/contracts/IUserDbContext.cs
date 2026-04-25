
using CoachingFit.User.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoachingFit.User.Core.Contracts
{
    public interface IUserDbContext
    {
        DbSet<CoachProfile> CoachProfiles { get; }
        DbSet<TraineeProfile> TraineeProfiles { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
