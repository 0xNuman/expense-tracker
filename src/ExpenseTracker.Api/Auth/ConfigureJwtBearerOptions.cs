using ExpenseTracker.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Post-configures <see cref="JwtBearerOptions"/> by resolving the singleton
/// <see cref="IAccessTokenService"/> for token validation parameters. This is
/// the standard ASP.NET pattern for options that depend on other DI services.
/// </summary>
internal sealed class ConfigureJwtBearerOptions : IConfigureOptions<JwtBearerOptions>
{
    private readonly IAccessTokenService _tokenService;

    public ConfigureJwtBearerOptions(IAccessTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = ((JwtAccessTokenService)_tokenService).ValidationParameters;
        options.MapInboundClaims = false;
    }
}