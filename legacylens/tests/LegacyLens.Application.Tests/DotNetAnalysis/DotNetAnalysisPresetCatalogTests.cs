using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Application.Tests.DotNetAnalysis;

public sealed class DotNetAnalysisPresetCatalogTests
{
    [Fact]
    public void Resolve_ReturnsAllPresets_WhenNoPresetIdsProvided()
    {
        var catalog = new DotNetAnalysisPresetCatalog();

        var presets = catalog.Resolve(null);

        Assert.NotEmpty(presets);
    }

    [Fact]
    public void Resolve_ThrowsForUnknownPreset()
    {
        var catalog = new DotNetAnalysisPresetCatalog();

        Assert.Throws<ArgumentException>(() => catalog.Resolve(["missing"]));
    }
}
