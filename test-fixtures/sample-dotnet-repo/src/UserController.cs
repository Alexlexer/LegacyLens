namespace SampleApp;

public sealed class UserController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<User?> GetAsync(int id)
    {
        return await _userService.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<User>> ListAsync()
    {
        return await _userService.GetAllAsync();
    }

    public async Task<User> CreateAsync(string name, string email)
    {
        return await _userService.CreateAsync(name, email);
    }
}
