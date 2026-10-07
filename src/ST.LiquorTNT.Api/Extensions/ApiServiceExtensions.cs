using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ST.LiquorTNT.Api.Logging;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Infrastructure;

namespace ST.LiquorTNT.Api.Extensions;

public static class ApiServiceExtensions
{
    public const string WebCorsPolicy = "web";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            // MVC must not write its own ProblemDetails for 404/415 …: ExceptionMiddleware gives them errorCode.
            options.SuppressMapClientErrors = true;

            // A body that cannot be bound is an error like any other: it goes through ExceptionMiddleware
            // so the client always gets RFC 7807 + errorCode + correlationId, never MVC's own shape.
            options.InvalidModelStateResponseFactory = context =>
            {
                // Several ModelState keys can name the same field (broken JSON reports "$" and "request"),
                // so messages are grouped per field instead of assuming one key each.
                var errors = context.ModelState
                    .Where(e => e.Value is { Errors.Count: > 0 })
                    .GroupBy(e => FieldName(e.Key))
                    .ToDictionary(
                        g => g.Key,
                        g => g.SelectMany(e => e.Value!.Errors)
                              .Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage)
                              .Distinct()
                              .ToArray());

                throw new ValidationException(errors);
            };
        });
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IRequestContext, RequestContext>();

        services.AddBusiness();
        services.AddMethodLogging(typeof(Business.DependencyInjection).Assembly);   // DETAIL mode: method input/output
        services.AddHostedService<LogModeRefresher>();                             // LOG_MODE from SECURITY_CONFIG
        services.AddInfrastructure(configuration);

        AddJwtAuthentication(services, configuration);
        AddCors(services, configuration);
        AddSwagger(services);

        return services;
    }

    /// <summary>"$.roleId" / "request.RoleId" / "RoleId" -> "roleId", so it matches the FluentValidation field keys.</summary>
    private static string FieldName(string modelStateKey)
    {
        var key = modelStateKey.TrimStart('$', '.');
        var dot = key.LastIndexOf('.');
        key = dot >= 0 ? key[(dot + 1)..] : key;

        return string.IsNullOrEmpty(key) ? "request" : char.ToLowerInvariant(key[0]) + key[1..];
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "ST.LiquorTNT";
        var audience = configuration["Jwt:Audience"] ?? "ST.LiquorTNT.Web";
        var signingKey = configuration["Jwt:SigningKey"]
                         ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (!context.Request.Headers.ContainsKey("Authorization"))
                            {
                                context.Token = AuthCookies.ReadJwt(context.Request);
                            }

                            return Task.CompletedTask;
                        },

                        // A missing/invalid token gets the same RFC 7807 shape as every other error.
                        // The JWT ends at the session's hard limit, so an expired JWT is SESSION_EXPIRED
                        // (password popup + retry), exactly like the session check says it.
                        OnChallenge = context =>
                        {
                            context.HandleResponse();

                            if (context.AuthenticateFailure is SecurityTokenExpiredException)
                            {
                                throw new UnauthorizedException(ErrorCodes.SessionExpired, "This session has reached its time limit.",
                                    "Enter your password again to continue.");
                            }

                            throw new UnauthorizedException(ErrorCodes.Unauthenticated, "Authentication is required.",
                                string.IsNullOrEmpty(context.ErrorDescription) ? "Log in first (browser) or send a valid bearer token." : context.ErrorDescription);
                        },
                    };
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = "unique_name",
                    };
                });

        services.Configure<AuthCookieOptions>(configuration.GetSection(AuthCookieOptions.Section));

        // [HasPermission("user.add")]: policies are built per key on the fly and checked against the
        // caller's roles and custom rights in the selected supplier code (see Security/PermissionAuthorization.cs).
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" };

        services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy =>
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "ST.LiquorTNT API", Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste only the token from POST /api/auth/login. Swagger adds the \"Bearer \" prefix. "
                              + "(The web application uses the HttpOnly jwt cookie set by the same call instead.)",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });
    }
}
