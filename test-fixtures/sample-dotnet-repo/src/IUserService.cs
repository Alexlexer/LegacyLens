namespace SampleApp;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string name, string email, CancellationToken cancellationToken = default);
}
