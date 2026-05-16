namespace LegacyLens.Application.Audit;

internal static class AuditFileSystemHelpers
{
    public static IReadOnlyList<string> EnumerateFiles(string root)
    {
        try
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var parts = f.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
                    return !parts.Any(p =>
                        string.Equals(p, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "obj", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, ".git", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p, "packages", StringComparison.OrdinalIgnoreCase));
                })
                .ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    public static IReadOnlyList<string> FindDirectories(string root, string name)
    {
        try
        {
            return Directory.GetDirectories(root, name, SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    public static bool HasTestProjects(string root)
    {
        try
        {
            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Any(f =>
                    f.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith("Spec.cs", StringComparison.OrdinalIgnoreCase))
                || Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                    .Any(d =>
                    {
                        var name = Path.GetFileName(d);
                        return name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
                               name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
                               name.Contains("Tests", StringComparison.OrdinalIgnoreCase);
                    });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
