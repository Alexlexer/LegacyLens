using RefactorGuard.Application;
using RefactorGuard.Application.Audit;
using RefactorGuard.Application.DotNetAnalysis;
using RefactorGuard.Application.Git;
using RefactorGuard.Application.Reports;
using RefactorGuard.Application.Review;
using RefactorGuard.Application.Search;
using RefactorGuard.Domain.Common;
using RefactorGuard.Domain.Git;
using RefactorGuard.Infrastructure;
using RefactorGuard.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRefactorGuardApplication();
builder.Services.AddRefactorGuardInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(SystemHealth.Healthy()));
app.MapGet("/api/search/status", async (
    GpuSearchStatusWorkflow workflow,
    CancellationToken cancellationToken) =>
{
    var status = await workflow.GetStatusAsync(cancellationToken);
    return Results.Ok(status);
});
app.MapPost("/api/audit/legacy-dotnet", async (
    LegacyAuditRequest request,
    ILegacyAuditOrchestrator auditOrchestrator,
    IRepoPathValidator repoPathValidator,
    IReportRepository reportRepository,
    CancellationToken cancellationToken) =>
{
    try
    {
        var repoPath = repoPathValidator.Validate(request.RepoPath ?? string.Empty);
        var report = await auditOrchestrator.AuditAsync(request with { RepoPath = repoPath }, cancellationToken);

        try
        {
            await reportRepository.SaveAuditAsync(report, cancellationToken);
        }
        catch (Exception)
        {
            // Persistence failure does not fail the audit response
        }

        return Results.Ok(report);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-audit-request",
            "Invalid audit request",
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
    catch (DirectoryNotFoundException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/repo-not-found",
            "Repository path not found",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
});
app.MapPost("/api/dotnet/analyze", async (
    DotNetAnalysisRequest request,
    DotNetAnalysisService analysisService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await analysisService.AnalyzeAsync(request, cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-dotnet-analysis-request",
            "Invalid .NET analysis request",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
    catch (HttpRequestException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/gpu-search-unavailable",
            "gpu-search-mcp unavailable",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
});
app.MapPost("/api/dotnet/workspace/scan", async (
    DotNetWorkspaceScanRequest request,
    IRepoPathValidator repoPathValidator,
    IRoslynSymbolScanner symbolScanner,
    CancellationToken cancellationToken) =>
{
    try
    {
        var repoPath = repoPathValidator.Validate(request.RepoPath ?? string.Empty);
        var response = await symbolScanner.ScanAsync(repoPath, cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-dotnet-workspace-scan-request",
            "Invalid .NET workspace scan request",
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
});
app.MapPost("/api/dotnet/references", async (
    RoslynReferenceAnalysisRequest request,
    IRepoPathValidator repoPathValidator,
    IRoslynReferenceAnalyzer referenceAnalyzer,
    CancellationToken cancellationToken) =>
{
    try
    {
        var repoPath = repoPathValidator.Validate(request.RepoPath ?? string.Empty);
        var response = await referenceAnalyzer.FindReferencesAsync(
            request with { RepoPath = repoPath },
            cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ProblemDetailsResponse(
            "https://refactorguard.local/errors/invalid-dotnet-reference-request",
            "Invalid .NET reference analysis request",
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
});
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
app.MapPost("/api/review/diff", async (
    DiffReviewRequest request,
    IReviewOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    try
    {
        var report = await orchestrator.ReviewDiffAsync(request, cancellationToken);
        return Results.Ok(report);
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
            "https://refactorguard.local/errors/diff-review-failed",
            "Unable to review Git diff",
            StatusCodes.Status400BadRequest,
            ex.Message));
    }
});
app.MapGet("/api/reports", async (
    IReportRepository reports,
    string? type,
    CancellationToken cancellationToken) =>
{
    var result = await reports.ListAsync(cancellationToken);
    if (!string.IsNullOrEmpty(type))
        result = result.Where(r => string.Equals(r.ReportType, type, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(result);
});
app.MapGet("/api/reports/{id}", async (
    string id,
    IReportRepository reports,
    CancellationToken cancellationToken) =>
{
    var report = await reports.GetByIdAsync(id, cancellationToken);
    return report is null ? Results.NotFound() : Results.Ok(report);
});
app.MapDelete("/api/reports/{id}", async (
    string id,
    IReportRepository reports,
    CancellationToken cancellationToken) =>
{
    var deleted = await reports.DeleteAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});
app.MapGet("/api/audit/reports/{id}", async (
    string id,
    IReportRepository reports,
    CancellationToken cancellationToken) =>
{
    var report = await reports.GetAuditByIdAsync(id, cancellationToken);
    return report is null ? Results.NotFound() : Results.Ok(report);
});

app.Run();

public partial class Program;
