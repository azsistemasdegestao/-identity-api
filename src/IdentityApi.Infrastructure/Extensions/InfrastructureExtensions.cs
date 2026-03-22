using IdentityApi.Application.Services;
using IdentityApi.Domain.Repositories;
using IdentityApi.Domain.Services;
using IdentityApi.Infrastructure.Data;
using IdentityApi.Infrastructure.Repositories;
using IdentityApi.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IdentityApi.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtKey = config["Jwt:KeyAccessToken"] ?? throw new InvalidOperationException("Jwt:KeyAccessToken not configured.");
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // MassTransit
        var serviceBusConnectionString = config["AzureServiceBus:ConnectionString"];
        services.AddMassTransit(x =>
        {
            if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
            {
                x.UsingAzureServiceBus((ctx, cfg) =>
                {
                    cfg.Host(serviceBusConnectionString);
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingInMemory((ctx, cfg) =>
                {
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        // Register services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IUserApplicationService, UserApplicationService>();

        return services;
    }
}
