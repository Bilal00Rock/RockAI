using RockAI.Application.Common.Interfaces;
using RockAI.Infrastructure.Common.Persistence;
using RockAI.Infrastructure.Messages.Persistence;
using RockAI.Infrastructure.Conversations.Persistence;
using RockAI.Infrastructure.Users.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RockAI.Infrastructure.Authentication.TokenGenerator;
using Microsoft.Extensions.Options;
using RockAI.Domain.Common.Interfaces;
using RockAI.Infrastructure.Authentication.PasswordHasher;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace RockAI.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddAuthentication(configuration)
            .AddPersistence();
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContext<RockAIDbContext>(options =>
            options.UseSqlite("Data Source = RockAI.db"));

        services.AddScoped<IMessagesRepository, MessagesRepository>();
        services.AddScoped<IConversationsRepository, ConversationsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<RockAIDbContext>());

        return services;
    }
     public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        //var jwtSettings = new JwtSettings();
        //configuration.Bind(JwtSettings.Section, jwtSettings);

        //services.AddSingleton(Options.Create(jwtSettings));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        //services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
        //    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
        //    {
        //        ValidateIssuer = true,
        //        ValidateAudience = true,
        //        ValidateLifetime = true,
        //        ValidateIssuerSigningKey = true,
        //        ValidIssuer = jwtSettings.Issuer,
        //        ValidAudience = jwtSettings.Audience,
        //        IssuerSigningKey = new SymmetricSecurityKey(
        //            Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        //    });


        return services;
    }
}