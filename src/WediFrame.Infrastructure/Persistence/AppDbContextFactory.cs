using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WediFrame.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef" tooling (migrations). Runtime configuration lives in the API host.
/// Connection string resolution order:
///   1. WEDIFRAME_DB environment variable
///   2. local development default (docker/local PostgreSQL)
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("WEDIFRAME_DB")
            ?? "Host=localhost;Port=5432;Database=wediframe;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
