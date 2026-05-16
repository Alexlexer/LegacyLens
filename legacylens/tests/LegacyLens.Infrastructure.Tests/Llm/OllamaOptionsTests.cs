using System.ComponentModel.DataAnnotations;
using LegacyLens.Infrastructure.Llm;

namespace LegacyLens.Infrastructure.Tests.Llm;

public sealed class OllamaOptionsTests
{
    [Fact]
    public void Validation_FailsWhenModelIsMissing()
    {
        var options = new OllamaOptions { Model = string.Empty };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OllamaOptions.Model)));
    }

    [Fact]
    public void Validation_FailsWhenBaseUrlIsMissing()
    {
        var options = new OllamaOptions { BaseUrl = null! };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OllamaOptions.BaseUrl)));
    }

    [Fact]
    public void Validation_FailsWhenTimeoutIsNotPositive()
    {
        var options = new OllamaOptions { TimeoutSeconds = 0 };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OllamaOptions.TimeoutSeconds)));
    }

    private static List<ValidationResult> Validate(OllamaOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);
        return results;
    }
}
