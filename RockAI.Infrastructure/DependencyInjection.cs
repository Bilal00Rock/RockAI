using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Common.Interfaces;
using RockAI.Infrastructure.Common.Persistence;
using RockAI.Infrastructure.Messages.Persistence;
using RockAI.Infrastructure.Conversations.Persistence;
using RockAI.Infrastructure.Users.Persistence;
using RockAI.Domain.Common.Interfaces;
using RockAI.Infrastructure.Authentication.PasswordHasher;

namespace RockAI.Infrastructure;

public static class DependencyInjection
{
    // MAUI / client-side infrastructure registration.
    // This project must not register ASP.NET Core server authentication or JWT generation.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services
            .AddPersistence();
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // Use a local SQLite database for the MAUI application.
        services.AddDbContext<RockAIDbContext>(options =>
            options.UseSqlite("Data Source=RockAI.db"));

        services.AddScoped<IMessagesRepository, MessagesRepository>();
        services.AddScoped<IConversationsRepository, ConversationsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();

        // Register the DbContext as the unit of work implementation
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<RockAIDbContext>());

        // Password hasher (for local seeding and optional client-side hashing)
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Database initializer to ensure database creation and seed default data
        services.AddTransient<Common.Persistence.DatabaseInitializer>();

        return services;
    }
}
