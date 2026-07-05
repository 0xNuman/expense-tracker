using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Hal;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Auth;
using ExpenseTracker.Infrastructure.Email;
using ExpenseTracker.Infrastructure.Persistence;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Api.Features.Auth;

/// <summary>Registers all auth endpoints on the route group /api/auth.</summary>
public static class AuthEndpoints
{
    private sealed class LogCategory { }

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .ExcludeFromDescription();

        group.MapPost("/magic-link", RequestMagicLink)
             .WithName("RequestMagicLink")
             .WithSummary("Request a magic-link login email.")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/magic-link/verify", VerifyMagicLink)
             .WithName("VerifyMagicLink")
             .WithSummary("Verify a magic-link token and issue tokens.")
             .Produces<TokenResponse>(StatusCodes.Status200OK, contentType: HalDocument.MediaType)
             .ProducesProblem(StatusCodes.Status410Gone);

        group.MapPost("/refresh", Refresh)
             .WithName("RefreshToken")
             .WithSummary("Rotate the refresh cookie and issue a new access token.")
             .Produces<TokenResponse>(StatusCodes.Status200OK, contentType: HalDocument.MediaType)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

group.MapPost("/switch-tenant", SwitchTenant)
              .WithName("SwitchTenant")
              .WithSummary("Switch the active tenant and re-issue the access token.")
              .RequireAuthorization()
              .Produces<TokenResponse>(StatusCodes.Status200OK, contentType: HalDocument.MediaType)
              .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/passkeys/begin-registration", BeginPasskeyRegistration)
              .WithName("BeginPasskeyRegistration")
              .WithSummary("Begin WebAuthn (passkey) registration for the current user.")
              .RequireAuthorization()
              .Produces(StatusCodes.Status200OK, contentType: "application/json")
              .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/passkeys/complete-registration", CompletePasskeyRegistration)
              .WithName("CompletePasskeyRegistration")
              .WithSummary("Verify the attestation response and store the passkey credential.")
              .RequireAuthorization()
              .Produces<HalDocument>(StatusCodes.Status200OK, contentType: HalDocument.MediaType)
              .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/passkeys/begin-auth", BeginPasskeyAuth)
              .WithName("BeginPasskeyAuth")
              .WithSummary("Begin WebAuthn (passkey) assertion; returns challenge + session id.")
              .Produces(StatusCodes.Status200OK, contentType: "application/json")
              .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/passkeys/complete-auth", CompletePasskeyAuth)
              .WithName("CompletePasskeyAuth")
              .WithSummary("Verify the assertion response and issue access + refresh tokens.")
              .Produces<TokenResponse>(StatusCodes.Status200OK, contentType: HalDocument.MediaType)
              .ProducesProblem(StatusCodes.Status401Unauthorized)
              .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    // ── POST /api/auth/magic-link ──────────────────────────────────
    private static async Task<IResult> RequestMagicLink(
        MagicLinkRequest request,
        ExpenseTrackerDbContext db,
        IEmailSender emailSender,
        IOptions<EmailSenderOptions> emailOptions,
        AuthRateLimiter rateLimiter,
        LinkGenerator linker,
        HttpContext ctx,
        CancellationToken ct)
    {
        // Validate email minimally (same response for valid/invalid/no-such-user).
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return Results.NoContent();

        var email = request.Email.Trim();

        // Rate limit: 5 magic-link requests per email per hour.
        var partitionKey = $"ml:{email.ToUpperInvariant()}";
        if (!rateLimiter.TryConsume(partitionKey, maxRequests: 5, window: TimeSpan.FromHours(1)))
        {
            var retryAfter = rateLimiter.GetRetryAfterSeconds(partitionKey, TimeSpan.FromHours(1));
            ctx.Response.Headers.RetryAfter = retryAfter.ToString();
            return Results.Problem("Too many magic-link requests. Try again later.", statusCode: StatusCodes.Status429TooManyRequests);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var normalizedEmail = email.ToUpperInvariant();

        // Check if user exists (we do NOT create the user here; verification does that).
        var existingUser = await db.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        var userForToken = existingUser ?? User.Invite(email);

        if (existingUser is null)
            db.Users.Add(userForToken);

        var ip = ctx.Connection.RemoteIpAddress?.ToString();

        var (token, rawToken) = MagicLinkToken.Issue(email, nowUtc, ip);
        db.MagicLinkTokens.Add(token);
        await db.SaveChangesAsync(ct);

        // Build the verification URL (deep link to the client app).
        var verifyPath = linker.GetPathByName("VerifyMagicLink", values: null) ?? "/api/auth/magic-link/verify";
        var baseOrigin = ctx.Request.Host.Host == "localhost"
            ? "http://localhost:5173"
            : $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var linkUrl = $"{baseOrigin}/login-complete?token={rawToken}";

        var html = $"""
            <!doctype html><html><body style="font-family:system-ui,sans-serif;max-width:520px;margin:24px auto">
            <h2>Sign in to Expense Tracker</h2>
            <p>Click the button below to complete your sign-in. This link expires in 15 minutes.</p>
            <p><a href="{linkUrl}" style="display:inline-block;background:#0f172a;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none">Sign in</a></p>
            <p style="color:#64748b;font-size:13px">If you didn't request this link, you can safely ignore this email.</p>
            </body></html>
            """;
        var text = $"Sign in to Expense Tracker:\n{linkUrl}\n\nThis link expires in 15 minutes.";

        try
        {
            await emailSender.SendAsync(new EmailMessage(email, "Sign in to Expense Tracker", html, text), ct);
        }
        catch (Exception ex)
        {
            // Dev: log the link so tests can proceed without an SMTP server.
            var logger = ctx.RequestServices.GetRequiredService<ILogger<LogCategory>>();
            logger.LogWarning(ex, "Failed to send magic-link email to {Email}. Dev link: {LinkUrl}", email, linkUrl);
        }

        return Results.NoContent();
    }

    // ── POST /api/auth/magic-link/verify ───────────────────────────
    private static async Task<IResult> VerifyMagicLink(
        VerifyMagicLinkRequest request,
        ExpenseTrackerDbContext db,
        IAccessTokenService tokenService,
        HttpResponse response,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Results.Problem("Token is required.", statusCode: StatusCodes.Status400BadRequest);

        var nowUtc = DateTimeOffset.UtcNow;
        var hash = MagicLinkToken.HashRaw(request.Token);

        var token = await db.MagicLinkTokens
            .AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || token.IsConsumed)
            return Results.Problem("Token is no longer valid.", statusCode: StatusCodes.Status410Gone, title: "Token consumed");
        if (token.IsExpired(nowUtc))
            return Results.Problem("Token has expired.", statusCode: StatusCodes.Status410Gone, title: "Token expired");

        var normalizedEmail = token.NormalizedEmail;
        var user = await db.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        // A user is "first login" if they don't exist yet OR were created as a
        // pending invite by the magic-link request handler. In both cases we
        // register/confirm them and bootstrap a Personal tenant (per US-1.1).
        var isFirstLogin = user is null || user.IsPending;

        if (user is null)
        {
            user = User.Register(token.Email, displayName: string.Empty);
            db.Users.Add(user);
        }
        else
        {
            user.ConfirmEmail();
        }

        if (isFirstLogin)
        {
            var tenant = Tenant.Create("Personal", user!.Id);
            db.Tenants.Add(tenant);
            // Explicitly add memberships to ensure the change tracker persists them
            // alongside the tenant (IReadOnlyCollection navigation isn't auto-detected).
            foreach (var m in tenant.Memberships)
                db.TenantMemberships.Add(m);

            db.Set<Category>().AddRange(
                Category.Create(tenant.Id, "Food", CategoryKind.Expense, icon: "Utensils", sortOrder: 1),
                Category.Create(tenant.Id, "Transport", CategoryKind.Expense, icon: "Car", sortOrder: 2),
                Category.Create(tenant.Id, "Utilities", CategoryKind.Expense, icon: "Zap", sortOrder: 3),
                Category.Create(tenant.Id, "Salary", CategoryKind.Income, icon: "Banknote", sortOrder: 4)
            );
        }

        // Mark token consumed.
        token.Consume(user!.Id, nowUtc);
        await db.SaveChangesAsync(ct);

        // Find the user's first tenant membership to issue the access token with.
        var membership = await db.TenantMemberships
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id, ct);

        var tenantName = "Personal";
        if (membership is not null)
        {
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == membership.TenantId, ct);
            tenantName = tenant?.Name ?? "Personal";
        }

        var claims = new AccessTokenClaims(
            user.Id,
            user.Email,
            membership?.TenantId,
            membership?.Role,
            new[] { "read", "write" });

        var accessResult = tokenService.Issue(claims, nowUtc);

        // Issue refresh token.
        var (refresh, rawRefresh) = RefreshToken.IssueFor(user.Id, nowUtc, "Magic link");
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);

        response.SetRefreshCookie(rawRefresh, refresh.ExpiresAtUtc);

        var body = new TokenResponse
        {
            AccessToken = accessResult.Token,
            ExpiresAtUtc = accessResult.ExpiresAtUtc,
            TenantId = membership?.TenantId.ToString() ?? string.Empty,
            TenantName = tenantName,
            Email = user.Email
        };

        var hal = new HalDocument()
            .WithLink("self", Link.Post("/api/auth/magic-link/verify"))
            .WithLink("refresh", Link.Post("/api/auth/refresh"))
            .WithLink("switch-tenant", Link.Post("/api/auth/switch-tenant"))
            .WithLink("et:tenant", Link.Get($"/api/tenants/{membership?.TenantId}"))
            .WithState("accessToken", body.AccessToken)
            .WithState("expiresAtUtc", body.ExpiresAtUtc)
            .WithState("tenantId", body.TenantId)
            .WithState("tenantName", body.TenantName)
            .WithState("email", body.Email);

        return Results.Extensions.Hal(hal);
    }

    // ── POST /api/auth/refresh ─────────────────────────────────────
    private static async Task<IResult> Refresh(
        HttpRequest request,
        HttpResponse response,
        ExpenseTrackerDbContext db,
        IAccessTokenService tokenService,
        CancellationToken ct)
    {
        var rawRefresh = request.GetRefreshCookie();
        if (string.IsNullOrEmpty(rawRefresh))
            return Results.Problem("Refresh token missing.", statusCode: StatusCodes.Status401Unauthorized);

        var nowUtc = DateTimeOffset.UtcNow;
        var hash = RefreshToken.HashRaw(rawRefresh);

        var stored = await db.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return Results.Problem("Invalid refresh token.", statusCode: StatusCodes.Status401Unauthorized);

        // Reuse detection: if already revoked, the family is compromised.
        if (stored.RevokedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(db, stored.FamilyId, nowUtc, ct);
            response.ClearRefreshCookie();
            return Results.Problem("Token reuse detected; session revoked.", statusCode: StatusCodes.Status401Unauthorized);
        }

        if (nowUtc >= stored.ExpiresAtUtc)
        {
            stored.Revoke(nowUtc);
            await db.SaveChangesAsync(ct);
            response.ClearRefreshCookie();
            return Results.Problem("Refresh token expired.", statusCode: StatusCodes.Status401Unauthorized);
        }

        // Rotate.
        var (replacement, rawReplacement) = stored.Rotate(nowUtc);
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        response.SetRefreshCookie(rawReplacement, replacement.ExpiresAtUtc);

        // Look up the user's current tenant membership (if any).
        var membership = await db.TenantMemberships
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == stored.UserId, ct);

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);

        var claims = new AccessTokenClaims(
            stored.UserId,
            user?.Email ?? string.Empty,
            membership?.TenantId,
            membership?.Role,
            new[] { "read", "write" });

        var accessResult = tokenService.Issue(claims, nowUtc);

        var hal = new HalDocument()
            .WithLink("self", Link.Post("/api/auth/refresh"))
            .WithLink("switch-tenant", Link.Post("/api/auth/switch-tenant"))
            .WithState("accessToken", accessResult.Token)
            .WithState("expiresAtUtc", accessResult.ExpiresAtUtc)
            .WithState("email", claims.Email);

        return Results.Extensions.Hal(hal);
    }

    // ── POST /api/auth/switch-tenant ───────────────────────────────
    [Microsoft.AspNetCore.Authorization.Authorize]
    private static async Task<IResult> SwitchTenant(
        SwitchTenantRequest request,
        ICurrentUserService currentUser,
        ExpenseTrackerDbContext db,
        IAccessTokenService tokenService,
        CancellationToken ct)
    {
        if (!Guid.TryParse(request.TenantId, out var tid))
            return Results.Problem("Invalid tenantId.", statusCode: StatusCodes.Status400BadRequest);
        if (!currentUser.UserId.HasValue)
            return Results.Problem("Not authenticated.", statusCode: StatusCodes.Status401Unauthorized);

        var nowUtc = DateTimeOffset.UtcNow;
        var targetTenantId = new TenantId(tid);

        var membership = await db.TenantMemberships
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == currentUser.UserId.Value && m.TenantId == targetTenantId, ct);

        if (membership is null)
            return Results.Problem("You are not a member of that tenant.", statusCode: StatusCodes.Status403Forbidden);

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == targetTenantId, ct);
        var tenantName = tenant?.Name ?? "Unknown";

        var claims = new AccessTokenClaims(
            currentUser.UserId.Value,
            currentUser.Email,
            membership.TenantId,
            membership.Role,
            new[] { "read", "write" });

        var accessResult = tokenService.Issue(claims, nowUtc);

        var hal = new HalDocument()
            .WithLink("self", Link.Post("/api/auth/switch-tenant"))
            .WithLink("refresh", Link.Post("/api/auth/refresh"))
            .WithLink("et:tenant", Link.Get($"/api/tenants/{targetTenantId}"))
            .WithState("accessToken", accessResult.Token)
            .WithState("expiresAtUtc", accessResult.ExpiresAtUtc)
            .WithState("tenantId", targetTenantId.ToString())
            .WithState("tenantName", tenantName);

        return Results.Extensions.Hal(hal);
    }

    private static async Task RevokeFamilyAsync(ExpenseTrackerDbContext db, Guid familyId, DateTimeOffset atUtc, CancellationToken ct)
    {
        var family = await db.RefreshTokens
            .AsTracking()
            .Where(t => t.FamilyId == familyId)
            .ToListAsync(ct);

        foreach (var t in family)
            t.Revoke(atUtc);

        await db.SaveChangesAsync(ct);
    }

    // ── Passkey (WebAuthn) helpers ─────────────────────────────────

    private const int PasskeyRateLimit = 10;
    private static readonly TimeSpan PasskeyRateWindow = TimeSpan.FromMinutes(5);

    private static string PasskeyIpPartition(HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"pk:{ip}";
    }

    private static string ToBase64Url(byte[] bytes)
    {
        var b64 = Convert.ToBase64String(bytes);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder > 0) padded = padded.PadRight(padded.Length + (4 - remainder), '=');
        return Convert.FromBase64String(padded);
    }

    private static IResult? RateLimited(HttpContext ctx, AuthRateLimiter rateLimiter, string partitionKey)
    {
        if (!rateLimiter.TryConsume(partitionKey, PasskeyRateLimit, PasskeyRateWindow))
        {
            var retryAfter = rateLimiter.GetRetryAfterSeconds(partitionKey, PasskeyRateWindow);
            ctx.Response.Headers.RetryAfter = retryAfter.ToString();
            return Results.Problem("Too many passkey requests. Try again later.", statusCode: StatusCodes.Status429TooManyRequests);
        }
        return null;
    }

    // ── POST /api/auth/passkeys/begin-registration ─────────────────
    [Microsoft.AspNetCore.Authorization.Authorize]
    private static async Task<IResult> BeginPasskeyRegistration(
        BeginPasskeyRegistrationRequest request,
        ICurrentUserService currentUser,
        ExpenseTrackerDbContext db,
        IFido2 fido2,
        IMemoryCache cache,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Results.Problem("Not authenticated.", statusCode: StatusCodes.Status401Unauthorized);

        var userId = currentUser.UserId.Value;

        var existingIds = await db.PasskeyCredentials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .Select(c => c.CredentialIdBase64Url)
            .ToListAsync(ct);

        var excludeCredentials = existingIds
            .Select(b => new PublicKeyCredentialDescriptor(FromBase64Url(b)))
            .ToList();

        var fido2User = new Fido2User
        {
            Id = userId.Value.ToByteArray(),
            Name = currentUser.Email,
            DisplayName = string.IsNullOrEmpty(currentUser.Email) ? userId.ToString() : currentUser.Email
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            AuthenticatorAttachment = null,
            ResidentKey = ResidentKeyRequirement.Preferred,
            UserVerification = UserVerificationRequirement.Preferred
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None,
            PubKeyCredParams = PubKeyCredParam.Defaults
        });

        cache.Set($"pk-reg:{userId}", options.ToJson(), TimeSpan.FromMinutes(5));

        return Results.Text(options.ToJson(), "application/json", System.Text.Encoding.UTF8);
    }

    // ── POST /api/auth/passkeys/complete-registration ─────────────
    [Microsoft.AspNetCore.Authorization.Authorize]
    private static async Task<IResult> CompletePasskeyRegistration(
        CompletePasskeyRegistrationRequest request,
        ICurrentUserService currentUser,
        ExpenseTrackerDbContext db,
        IFido2 fido2,
        IMemoryCache cache,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Results.Problem("Not authenticated.", statusCode: StatusCodes.Status401Unauthorized);
        if (request.AttestationResponse is null)
            return Results.Problem("Attestation response is required.", statusCode: StatusCodes.Status400BadRequest);

        var userId = currentUser.UserId.Value;
        if (!cache.TryGetValue<string>($"pk-reg:{userId}", out var json) || json is null)
            return Results.Problem("No pending passkey registration. Call begin-registration first.", statusCode: StatusCodes.Status400BadRequest);

        var options = CredentialCreateOptions.FromJson(json);

        var success = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = request.AttestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = (args, innerCt) =>
                IsCredentialIdUniqueToUser(args, userId, db, innerCt)
        }, ct);

        var nowUtc = DateTimeOffset.UtcNow;
        var deviceLabel = string.IsNullOrWhiteSpace(request.DeviceLabel) ? "Passkey" : request.DeviceLabel.Trim();

        var credential = new PasskeyCredential
        {
            Id = PasskeyCredentialId.New(),
            UserId = userId,
            CredentialIdBase64Url = ToBase64Url(success.Id),
            PublicKey = success.PublicKey,
            SignCount = success.SignCount,
            DeviceLabel = deviceLabel,
            CreatedAtUtc = nowUtc
        };

        db.PasskeyCredentials.Add(credential);
        await db.SaveChangesAsync(ct);

        var hal = new HalDocument()
            .WithLink("self", Link.Post("/api/auth/passkeys/complete-registration"))
            .WithLink("et:passkey-auth", Link.Post("/api/auth/passkeys/begin-auth", "Begin passkey sign-in"))
            .WithState("credentialId", credential.CredentialIdBase64Url)
            .WithState("deviceLabel", credential.DeviceLabel);

        return Results.Extensions.Hal(hal);
    }

    private static async Task<bool> IsCredentialIdUniqueToUser(
        IsCredentialIdUniqueToUserParams args,
        UserId userId,
        ExpenseTrackerDbContext db,
        CancellationToken ct)
    {
        var credentialB64 = ToBase64Url(args.CredentialId);
        var any = await db.PasskeyCredentials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(c => c.CredentialIdBase64Url == credentialB64 && c.UserId != userId, ct);
        return !any;
    }

    // ── POST /api/auth/passkeys/begin-auth ─────────────────────────
    private static async Task<IResult> BeginPasskeyAuth(
        BeginPasskeyAuthRequest request,
        ExpenseTrackerDbContext db,
        IFido2 fido2,
        AuthRateLimiter rateLimiter,
        IMemoryCache cache,
        HttpContext ctx,
        CancellationToken ct)
    {
        var rateResult = RateLimited(ctx, rateLimiter, PasskeyIpPartition(ctx));
        if (rateResult is not null) return rateResult;

        var credentialQuery = db.PasskeyCredentials
            .AsNoTracking()
            .IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToUpperInvariant();
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

            if (user is not null)
                credentialQuery = credentialQuery.Where(c => c.UserId == user.Id);
        }

        var credentialIds = await credentialQuery
            .Select(c => c.CredentialIdBase64Url)
            .ToListAsync(ct);

        var allowedCredentials = credentialIds
            .Select(b => new PublicKeyCredentialDescriptor(FromBase64Url(b)))
            .ToList();

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials,
            UserVerification = UserVerificationRequirement.Preferred
        });

        var sessionId = Guid.NewGuid().ToString("N");
        cache.Set($"pk-auth:{sessionId}", options.ToJson(), TimeSpan.FromMinutes(5));

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            sessionId,
            options = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(options.ToJson())
        });

        return Results.Text(payload, "application/json", System.Text.Encoding.UTF8);
    }

    // ── POST /api/auth/passkeys/complete-auth ──────────────────────
    private static async Task<IResult> CompletePasskeyAuth(
        CompletePasskeyAuthRequest request,
        ExpenseTrackerDbContext db,
        IAccessTokenService tokenService,
        IFido2 fido2,
        AuthRateLimiter rateLimiter,
        IMemoryCache cache,
        HttpResponse response,
        HttpContext ctx,
        CancellationToken ct)
    {
        var rateResult = RateLimited(ctx, rateLimiter, PasskeyIpPartition(ctx));
        if (rateResult is not null) return rateResult;

        if (request.AssertionResponse is null)
            return Results.Problem("Assertion response is required.", statusCode: StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(request.SessionId))
            return Results.Problem("Session id is required.", statusCode: StatusCodes.Status400BadRequest);

        if (!cache.TryGetValue<string>($"pk-auth:{request.SessionId}", out var json) || json is null)
            return Results.Problem("No pending passkey assertion. Call begin-auth first.", statusCode: StatusCodes.Status401Unauthorized);

        // Assertion options are single-use.
        cache.Remove($"pk-auth:{request.SessionId}");

        var options = AssertionOptions.FromJson(json);

        var credentialB64 = request.AssertionResponse.Id is null ? null : ToBase64Url(FromBase64Url(request.AssertionResponse.Id));
        var credential = credentialB64 is null ? null : await db.PasskeyCredentials
            .AsTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CredentialIdBase64Url == credentialB64, ct);

        if (credential is null)
            return Results.Problem("Unknown credential.", statusCode: StatusCodes.Status401Unauthorized);

        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = request.AssertionResponse,
            OriginalOptions = options,
            StoredPublicKey = credential.PublicKey,
            StoredSignatureCounter = credential.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, innerCt) =>
            {
                var owner = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == credential.UserId, innerCt);
                if (owner is null) return false;
                return args.UserHandle is null
                    ? false
                    : args.UserHandle.SequenceEqual(owner.Id.Value.ToByteArray());
            }
        }, ct);

        // Update sign count + last-used.
        credential.SignCount = result.SignCount;
        credential.LastUsedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var nowUtc = DateTimeOffset.UtcNow;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == credential.UserId, ct);

        if (user is null)
            return Results.Problem("Credential user not found.", statusCode: StatusCodes.Status401Unauthorized);

        var membership = await db.TenantMemberships
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id, ct);

        var tenantName = "Personal";
        if (membership is not null)
        {
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == membership.TenantId, ct);
            tenantName = tenant?.Name ?? "Personal";
        }

        var claims = new AccessTokenClaims(
            user.Id,
            user.Email,
            membership?.TenantId,
            membership?.Role,
            new[] { "read", "write" });

        var accessResult = tokenService.Issue(claims, nowUtc);

        var (refresh, rawRefresh) = RefreshToken.IssueFor(user.Id, nowUtc, "Passkey");
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);

        response.SetRefreshCookie(rawRefresh, refresh.ExpiresAtUtc);

        var hal = new HalDocument()
            .WithLink("self", Link.Post("/api/auth/passkeys/complete-auth"))
            .WithLink("refresh", Link.Post("/api/auth/refresh"))
            .WithLink("switch-tenant", Link.Post("/api/auth/switch-tenant"))
            .WithState("accessToken", accessResult.Token)
            .WithState("expiresAtUtc", accessResult.ExpiresAtUtc)
            .WithState("tenantId", membership?.TenantId.ToString() ?? string.Empty)
            .WithState("tenantName", tenantName)
            .WithState("email", user.Email);

        if (membership is not null)
            hal.WithLink("et:tenant", Link.Get($"/api/tenants/{membership.TenantId}"));

        return Results.Extensions.Hal(hal);
    }
}