namespace SampleApp;

// Illustrative SQL-style repository using fake/non-production connection strings.
public sealed class UserRepository : IUserRepository
{
    // Fake connection string — not a real credential.
    private const string ConnectionString = "Server=localhost;Database=SampleDb;User Id=app_user;Password=not-a-real-password;";

    private static readonly List<User> _store = [];

    public Task<User?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = _store.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<User>>(_store.AsReadOnly());
    }

    public Task<User> InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        var next = new User(_store.Count + 1, user.Name, user.Email, user.CreatedAt);
        _store.Add(next);
        return Task.FromResult(next);
    }
}
