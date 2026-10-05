using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.PasswordPolicies;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Business.Users;

namespace ST.LiquorTNT.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<PasswordPolicyValidator>();
        services.AddScoped<PasswordRules>();

        services.AddScoped<CredentialVerifier>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISecurityQuestionService, SecurityQuestionService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        services.AddScoped<ISecurityConfigService, SecurityConfigService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

        // Picks up every FluentValidation validator in this assembly as they are added.
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        return services;
    }
}
