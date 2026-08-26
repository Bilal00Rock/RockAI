using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using RockAI.Infrastructure.Common.Persistence;

namespace RockAI.Migrations;

public class RockAIDbContextFactory : IDesignTimeDbContextFactory<RockAIDbContext>
{
    public RockAIDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(RockAIDbContext).Assembly);
        });

        var serviceProvider = services.BuildServiceProvider();

        var optionsBuilder =
            new DbContextOptionsBuilder<RockAIDbContext>();

        optionsBuilder.UseSqlite(
            "Data Source=RockAI.db",
            sqlite => sqlite.MigrationsAssembly("RockAI.Migrations"));

        return new RockAIDbContext(
            optionsBuilder.Options,
            serviceProvider.GetRequiredService<IPublisher>());
    }
}