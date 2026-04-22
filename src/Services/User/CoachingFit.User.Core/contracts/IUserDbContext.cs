
namespace CoachingFit.User.Core.contracts
{
    public interface IUserDbContext
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
