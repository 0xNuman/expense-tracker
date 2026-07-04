using ExpenseTracker.Api.Auth;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Auth;
using ExpenseTracker.Infrastructure.Email;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api;

/// <summary>Wires authentication (JWT bearer) + email + auth services.</summary>
public static class AuthSetup
{
    public const string RefreshCookieName = "et_rt";

    public static IServiceCollection AddExpenseTrackerAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailSenderOptions>(configuration.GetSection(EmailSenderOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
        services.AddScoped<ITenantContext, RequestTenantContext>();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Create the token service eagerly so we can wire its validation parameters
        // into JwtBearer without a BuildServiceProvider() anti-pattern.
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var tokenService = new JwtAccessTokenService(Microsoft.Extensions.Options.Options.Create(jwtOptions));
        services.AddSingleton<IAccessTokenService>(tokenService);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenService.ValidationParameters;
                options.MapInboundClaims = false;
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>Writes the refresh token to an HttpOnly cookie.</summary>
    public static void SetRefreshCookie(this HttpResponse response, string rawToken, DateTimeOffset expiresAtUtc)
    {
        response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAtUtc,
            Path = "/api/auth",
            IsEssential = true
        });
    }

    /// <summary>Reads the refresh token from the cookie; null when absent.</summary>
    public static string? GetRefreshCookie(this HttpRequest request)
    {
        return request.Cookies.TryGetValue(RefreshCookieName, out var value) ? value : null;
    }

    /// <summary>Clears the refresh cookie.</summary>
    public static void ClearRefreshCookie(this HttpResponse response)
    {
        response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/auth" });
    }
}