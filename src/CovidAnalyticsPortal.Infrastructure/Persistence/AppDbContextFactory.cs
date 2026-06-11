using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CovidAnalyticsPortal.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used exclusively by the EF Core tooling
/// (<c>dotnet ef migrations</c> / <c>dotnet ef database update</c>) to construct
/// an <see cref="AppDbContext"/> without booting the full application host.
/// Because the tooling resolves this factory directly, the migration commands
/// never execute the API's start-up pipeline (including the automatic migration
/// step), keeping schema authoring side-effect free.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates an <see cref="AppDbContext"/> configured for the default SQLite
    /// provider, which is the database used to generate and store migrations.
    /// </summary>
    /// <param name="args">Arguments passed by the EF Core tooling (unused).</param>
    /// <returns>A configured <see cref="AppDbContext"/> instance.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=covidportal.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
