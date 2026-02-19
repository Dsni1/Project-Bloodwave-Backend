using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Extensions;

/// <summary>
/// Extension methods for service configuration and dependency injection
/// </summary>
public static class ServiceExtensions
{
    private const string DefaultJwtKey = "your-super-secret-key-that-must-be-at-least-32-characters-long-for-hmacsha256";
    private const string CorsPolicyName = "AllowAll";

    /// <summary>
    /// Adds CORS policy to the service collection
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        return services.AddCors(options =>
            options.AddPolicy(CorsPolicyName, policy =>
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
            )
        );
    }

    /// <summary>
    /// Adds database context configuration
    /// </summary>
    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return services.AddDbContext<BloodwaveDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        );
    }

    /// <summary>
    /// Configures JWT authentication
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = GetJwtKey(jwtSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services.AddAuthorization();
    }

    /// <summary>
    /// Adds Swagger documentation with JWT security definition
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Registers application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        return services;
    }

    /// <summary>
    /// Gets JWT key from configuration or returns default
    /// </summary>
    private static byte[] GetJwtKey(IConfigurationSection jwtSettings)
    {
        var keyString = jwtSettings["Key"] ?? DefaultJwtKey;
        return Encoding.UTF8.GetBytes(keyString);
    }
}
