using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ExpenseTracker.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Infrastructure.Auth;

/// <summary>
/// JWT access token issuer/validator using ECDSA P-256.
/// In dev, an ephemeral key is generated at construction; in prod, keys are
/// supplied via <see cref="JwtOptions.EcdsaPrivateKeyPem"/>.
/// </summary>
public sealed class JwtAccessTokenService : IAccessTokenService
{
    public const string UserIdClaimType = "et:uid";
    public const string TenantIdClaimType = "et:tid";
    public const string TenantRoleClaimType = "et:role";
    public const string ScopeClaimType = "scope";

    private readonly JwtOptions _options;
    private readonly ECDsa _signingKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtAccessTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        _signingKey = LoadOrCreateKey(_options);
        var securityKey = new ECDsaSecurityKey(_signingKey) { KeyId = _options.KeyId };
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

        _validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = securityKey,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    }

    public AccessTokenResult Issue(AccessTokenClaims claims, DateTimeOffset nowUtc)
    {
        var expiresAtUtc = nowUtc + _options.AccessTokenTtl;

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, claims.UserId.Value.ToString(), ClaimValueTypes.String));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, claims.Email, ClaimValueTypes.String));
        identity.AddClaim(new Claim(UserIdClaimType, claims.UserId.Value.ToString(), ClaimValueTypes.String));
        if (claims.ActiveTenantId.HasValue)
            identity.AddClaim(new Claim(TenantIdClaimType, claims.ActiveTenantId.Value.Value.ToString(), ClaimValueTypes.String));
        if (claims.RoleInActiveTenant.HasValue)
            identity.AddClaim(new Claim(TenantRoleClaimType, ((int)claims.RoleInActiveTenant.Value).ToString(), ClaimValueTypes.Integer32));
        foreach (var scope in claims.Scopes)
            identity.AddClaim(new Claim(ScopeClaimType, scope, ClaimValueTypes.String));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = identity,
            IssuedAt = nowUtc.UtcDateTime,
            NotBefore = nowUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = _signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return new AccessTokenResult(handler.WriteToken(token), expiresAtUtc);
    }

    public AccessTokenClaims? Validate(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out _);
            var userIdClaim = principal.FindFirst(UserIdClaimType)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new SecurityTokenException("Missing user id claim.");
            var userId = new UserId(Guid.Parse(userIdClaim));

            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

            TenantId? tenantId = null;
            var tenantClaim = principal.FindFirst(TenantIdClaimType)?.Value;
            if (Guid.TryParse(tenantClaim, out var tid))
                tenantId = new TenantId(tid);

            TenantRole? role = null;
            var roleClaim = principal.FindFirst(TenantRoleClaimType)?.Value;
            if (int.TryParse(roleClaim, out var roleInt))
                role = (TenantRole)roleInt;

            var scopes = principal.FindAll(ScopeClaimType).Select(c => c.Value).ToArray();

            return new AccessTokenClaims(userId, email, tenantId, role, scopes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Exposes validation parameters for bearer auth wiring in the API host.</summary>
    public TokenValidationParameters ValidationParameters => _validationParameters;

    private static ECDsa LoadOrCreateKey(JwtOptions options)
    {
        if (!string.IsNullOrEmpty(options.EcdsaPrivateKeyPem))
        {
            var key = ECDsa.Create();
            key.ImportFromPem(options.EcdsaPrivateKeyPem);
            return key;
        }
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }
}