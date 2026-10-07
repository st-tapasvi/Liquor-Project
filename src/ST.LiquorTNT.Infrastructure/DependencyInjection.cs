using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Companies;
using ST.LiquorTNT.Business.Excises;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.LiquorCategories;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Business.SupplierCodes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Infrastructure.Audit;
using ST.LiquorTNT.Infrastructure.Common;
using ST.LiquorTNT.Infrastructure.Database;
using ST.LiquorTNT.Infrastructure.Database.Repositories;
using ST.LiquorTNT.Infrastructure.Identity;

namespace ST.LiquorTNT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "MySql";
        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

        services.AddSingleton<SqlLoggingInterceptor>();     // logs the runnable query in DETAIL mode

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<SqlLoggingInterceptor>());

            switch (provider.ToLowerInvariant())
            {
                case "mysql":
                    // Declared explicitly: ServerVersion.AutoDetect would open a connection during startup.
                    var version = configuration["Database:ServerVersion"] ?? "8.0.36";
                    options.UseMySql(connectionString, new MySqlServerVersion(Version.Parse(version)));
                    break;

                case "sqlserver":
                    // SQL Server is a supported target. Its provider package is added when the first
                    // SQL Server customer is onboarded; the switch stays here so the decision is visible.
                    throw new NotSupportedException(
                        "SQL Server support is not wired yet. Add Microsoft.EntityFrameworkCore.SqlServer " +
                        "and call options.UseSqlServer(connectionString).");

                default:
                    throw new InvalidOperationException($"Unknown Database:Provider value '{provider}'.");
            }
        });

        // Access: effective rights and supplier codes
        services.AddScoped<IAccessRepository, AccessRepository>();

        // Users
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<IReferenceLookup, ReferenceLookup>();
        services.AddScoped<IPasswordPolicyRepository, PasswordPolicyRepository>();

        // Auth / sessions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ISecurityQuestionRepository, SecurityQuestionRepository>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();

        // Security administration
        services.AddScoped<ISecurityConfigRepository, SecurityConfigRepository>();

        // Roles and masters
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ILiquorCategoryRepository, LiquorCategoryRepository>();
        services.AddScoped<ISupplierCodeRepository, SupplierCodeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IExciseRepository, ExciseRepository>();

        // cross-cutting
        services.AddScoped<ISecurityConfigProvider, SecurityConfigProvider>();
        services.AddScoped<IUserLogWriter, UserLogWriter>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
