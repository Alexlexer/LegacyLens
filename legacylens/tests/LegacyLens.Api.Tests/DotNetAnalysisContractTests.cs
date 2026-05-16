using LegacyLens.Application.DotNetAnalysis;

namespace LegacyLens.Api.Tests;

public sealed class DotNetAnalysisContractTests
{
    [Fact]
    public void DotNetAnalysisResponse_ExposesStableContract()
    {
        var response = new DotNetAnalysisResponse(
            [new DotNetAnalysisPresetResult("async-blocking", "Async blocking calls", 1)],
            [new DotNetAnalysisFinding("async-blocking", "Review", "src/App.cs", 10, "Task.Result", "Rationale")]);

        Assert.Single(response.Presets);
        Assert.Single(response.Findings);
        Assert.Equal("async-blocking", response.Findings[0].PresetId);
    }
}
