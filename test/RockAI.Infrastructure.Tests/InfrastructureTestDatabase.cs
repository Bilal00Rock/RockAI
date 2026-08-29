using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using RockAI.Infrastructure.Common.Persistence;

namespace RockAI.Infrastructure.Tests;

public sealed class InfrastructureTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public InfrastructureTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RockAIDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new RockAIDbContext(options, Substitute.For<IPublisher>());
        Context.Database.EnsureCreated();
    }

    public RockAIDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
