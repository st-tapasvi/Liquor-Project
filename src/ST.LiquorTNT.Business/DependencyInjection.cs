using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Companies;
using ST.LiquorTNT.Business.Excises;
using ST.LiquorTNT.Business.LiquorCategories;
using ST.LiquorTNT.Business.PasswordPolicies;
using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Business.SupplierCodes;
using ST.LiquorTNT.Business.Users;

namespace ST.LiquorTNT.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Access: who may do what (one answer per request, kept for the whole request)
        services.AddScoped<CurrentAccess>();
        services.AddScoped<SupplierCodeDirectory>();
        services.AddScoped<IAccessService, AccessService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<UserAccessRules>();
        services.AddScoped<PasswordPolicyValidator>();
        services.AddScoped<PasswordRules>();

        services.AddScoped<CredentialVerifier>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISecurityQuestionService, SecurityQuestionService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        services.AddScoped<ISecurityConfigService, SecurityConfigService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

        // Roles and masters
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<RoleTemplates>();
        services.AddScoped<IRoleGroupService, RoleGroupService>();
        services.AddScoped<ILiquorCategoryService, LiquorCategoryService>();
        services.AddScoped<ISupplierCodeService, SupplierCodeService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IExciseService, ExciseService>();

        // Picks up every FluentValidation validator in this assembly as they are added.
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        return services;
    }
}
