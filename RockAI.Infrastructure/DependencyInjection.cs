using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Documents;
using RockAI.Domain.Common.Interfaces;
using RockAI.Infrastructure.Common.Persistence;
using RockAI.Infrastructure.Messages.Persistence;
using RockAI.Infrastructure.Conversations.Persistence;
using RockAI.Infrastructure.Users.Persistence;
using RockAI.Infrastructure.Authentication.PasswordHasher;
using RockAI.Infrastructure.Storage;
using RockAI.Infrastructure.Documents.Extractors;
using RockAI.Application.Attachments;

namespace RockAI.Infrastructure;

public static class DependencyInjection
{
    // MAUI / client-side infrastructure registration.
    // This project must not register ASP.NET Core server authentication or JWT generation.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(RockAIDbContext).Assembly);
        });

        return services.AddPersistence(databasePath);
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, string databasePath)
    {
        services.AddDbContext<RockAIDbContext>(options =>
        options.UseSqlite(
            $"Data Source={databasePath}",
            sqlite => sqlite.MigrationsAssembly("RockAI.Migrations")));

        services.AddScoped<IMessagesRepository, MessagesRepository>();
        services.AddScoped<IConversationsRepository, ConversationsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        // Register the DbContext as the unit of work implementation
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<RockAIDbContext>());

        // Password hasher (for local seeding and optional client-side hashing)
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Local file storage for attachments (local-first)
        services.AddSingleton<IFileStorageService>(sp =>
        {
            // Prefer MAUI FileSystem when available; fall back for tests/tools
            string root;
            try
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RockAI",
                    "attachments");
            }
            catch
            {
                root = Path.Combine(Path.GetTempPath(), "RockAI", "attachments");
            }
            return new LocalFileStorageService(root);
        });

        // Document understanding — extractors + processor
        services.AddSingleton<IFileContentExtractor, PlainTextExtractor>();
        services.AddSingleton<IFileContentExtractor, MarkdownExtractor>();
        services.AddSingleton<IFileContentExtractor, SourceCodeExtractor>();
        services.AddSingleton<IFileContentExtractor, StructuredTextExtractor>();
        services.AddSingleton<IFileContentExtractor, PdfTextExtractor>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        // Database initializer to ensure database creation and seed default data
        services.AddTransient<Common.Persistence.DatabaseInitializer>();

        return services;
    }
}
