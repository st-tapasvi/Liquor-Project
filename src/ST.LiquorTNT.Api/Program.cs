using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using ST.LiquorTNT.Api.Extensions;
using ST.LiquorTNT.Api.Logging;
using ST.LiquorTNT.Api.Middleware;
using ST.LiquorTNT.Logging;

// Keep claim names exactly as issued ("sub", "unique_name", "perm").
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Logging first: from here on, even a start-up failure reaches the log file.
builder.Host.UseAppLogging(builder.Configuration);

try
{
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    // One combined "API started" entry once the server is listening (so the URLs are known).
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses;
        LoggingSetup.LogStartup("API started", StartupFacts.From(app, urls is { Count: > 0 } ? urls : app.Urls));
    });

    // Correlation first, so every log line and every error carries the same reference.
    // Request logging wraps the exception middleware, so it records the final status and error body.
    app.UseMiddleware<CorrelationMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<CsrfProtectionMiddleware>();      // cookie-authenticated writes need X-XSRF-TOKEN

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.UseRequestInterceptor(
                "(req) => { const m = document.cookie.match(/(?:^|; )XSRF-TOKEN=([^;]*)/); "
                + "if (m) { req.headers['X-XSRF-TOKEN'] = decodeURIComponent(m[1]); } return req; }");
        });
    }

    app.UseCors(ApiServiceExtensions.WebCorsPolicy);

    app.UseAuthentication();
    app.UseMiddleware<SessionValidationMiddleware>();   // token is valid AND its session is still alive
    app.UseAuthorization();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

    app.Run();
    LoggingSetup.LogStopped();
}
catch (HostAbortedException)
{
    throw;      // EF tooling / the test host stopping the app on purpose — not a failure
}
catch (Exception ex)
{
    LoggingSetup.LogStartupFailure(ex);
    throw;      // IIS / the service manager must still see that the process failed
}

/// <summary>Exposed so ST.LiquorTNT.Api.Tests can reference the entry point.</summary>
public partial class Program
{
}
