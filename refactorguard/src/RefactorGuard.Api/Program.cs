using RefactorGuard.Application;
using RefactorGuard.Domain.Common;
using RefactorGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRefactorGuardApplication();
builder.Services.AddRefactorGuardInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/health"));
app.MapGet("/health", () => Results.Ok(SystemHealth.Healthy()));

app.Run();

public partial class Program;
