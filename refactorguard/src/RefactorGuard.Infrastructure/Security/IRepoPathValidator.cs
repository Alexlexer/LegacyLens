namespace RefactorGuard.Infrastructure.Security;

public interface IRepoPathValidator
{
    string Validate(string repoPath);
}
