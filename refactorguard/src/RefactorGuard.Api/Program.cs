using RefactorGuard.Application;
using RefactorGuard.Application.Git;
using RefactorGuard.Domain.Common;
using RefactorGuard.Domain.Git;
using RefactorGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRefactorGuardApplication();
builder.Services.AddRefactorGuardInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", () => Results.Ok(SystemHealth.Healthy()));
app.MapPost("/api/review/diff/preview", async (
    GitDiffPreviewRequest request,
    IGitDiffService gitDiffService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var preview = await gitDiffService.GetCurrentDiffAsync(request, cancellationToken);
        return Results.Ok(preview);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-request",
            "Invalid request",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-repo-path",
            "Invalid repository path",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/git-diff-failed",
            "Unable to preview Git diff",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
});

app.Run();

public partial class Program;
