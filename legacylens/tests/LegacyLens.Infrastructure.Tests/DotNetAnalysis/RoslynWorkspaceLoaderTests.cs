using LegacyLens.Infrastructure.DotNetAnalysis;

namespace LegacyLens.Infrastructure.Tests.DotNetAnalysis;

public sealed class RoslynWorkspaceLoaderTests
{
    [Fact]
    public void GetVisualStudioVersionPreference_PrefersStableMsBuild17BeforePreviewMsBuild18()
    {
        var stable2022 = RoslynWorkspaceLoader.GetVisualStudioVersionPreference(new Version(17, 14));
        var preview2026 = RoslynWorkspaceLoader.GetVisualStudioVersionPreference(new Version(18, 0));

        Assert.True(stable2022 > preview2026);
    }

    [Fact]
    public void GetVisualStudioVersionPreference_KeepsOlderStableMsBuildBeforePreviewMsBuild18()
    {
        var stable2019 = RoslynWorkspaceLoader.GetVisualStudioVersionPreference(new Version(16, 11));
        var preview2026 = RoslynWorkspaceLoader.GetVisualStudioVersionPreference(new Version(18, 0));

        Assert.True(stable2019 > preview2026);
    }
}
