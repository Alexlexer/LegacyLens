namespace SampleApp;

public interface IUserRepository
{
    Task<User?> FindAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);
    Task<User> InsertAsync(User user, CancellationToken cancellationToken = default);
}
