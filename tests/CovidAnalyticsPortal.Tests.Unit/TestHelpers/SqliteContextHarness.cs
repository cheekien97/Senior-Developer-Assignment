using CovidAnalyticsPortal.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CovidAnalyticsPortal.Tests.Unit.TestHelpers;

/// <summary>
/// Creates an <see cref="AppDbContext"/> backed by a private in-memory SQLite
/// database. A real relational provider is used (rather than the EF in-memory
/// provider) so the entity configurations, value converters and owned types are
/// genuinely exercised. The connection is kept open for the lifetime of the
/// harness and disposed alongside it.
/// </summary>
internal sealed class SqliteContextHarness : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Initializes the harness, opening the connection and creating the schema.
    /// </summary>
    public SqliteContextHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Gets the options bound to the shared in-memory connection.</summary>
    public DbContextOptions<AppDbContext> Options { get; }

    /// <summary>
    /// Creates a fresh context over the shared connection, mirroring the
    /// scoped-per-request lifetime used in production.
    /// </summary>
    public AppDbContext CreateContext() => new(Options);

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();
}
