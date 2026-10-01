using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Spins up a real PostgreSQL container (via Testcontainers) and boots the full ASP.NET Core
/// pipeline against it, so these tests exercise actual EF Core migrations, real SQL and the
/// whole middleware/auth pipeline, not an in-memory fake.
/// </summary>
public class TaskFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("taskflow_test")
        .WithUsername("taskflow")
        .WithPassword("taskflow")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Applied directly against the Testcontainers Postgres instance, using EF Core's real
        // migration runner (same migrations the app ships), rather than letting Program.cs do it.
        // Program.cs's own auto-migrate is skipped under the "Testing" environment (see below)
        // because WebApplicationFactory's host-builder discovery can invoke that top-level code
        // more than once, which would otherwise re-apply the same migration twice.
        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(_postgres.GetConnectionString());
        using var db = new TaskFlowDbContext(optionsBuilder.Options);
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "integration-test-secret-at-least-32-characters-long",
                ["Jwt:Issuer"] = "TaskFlow.Tests",
                ["Jwt:Audience"] = "TaskFlow.Tests",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        await db.Tasks.ExecuteDeleteAsync();
        await db.Projects.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
