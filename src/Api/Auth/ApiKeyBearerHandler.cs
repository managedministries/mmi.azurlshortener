using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public sealed class ApiKeyBearerOptions : AuthenticationSchemeOptions
{
    // Allow rotation: multiple valid keys
    public string[] ValidKeys { get; set; } = Array.Empty<string>();
}

public sealed class ApiKeyBearerHandler : AuthenticationHandler<ApiKeyBearerOptions>
{
    public ApiKeyBearerHandler(
        IOptionsMonitor<ApiKeyBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Expect: Authorization: Bearer <key>
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var authHeader = authHeaderValues.ToString();
        const string prefix = "Bearer ";

        if (!authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var presentedKey = authHeader.Substring(prefix.Length).Trim();

        if (string.IsNullOrWhiteSpace(presentedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing bearer token."));

        if (Options.ValidKeys is null || Options.ValidKeys.Length == 0)
            return Task.FromResult(AuthenticateResult.Fail("No API keys configured."));

        // Constant-time comparison helps avoid timing leaks
        var ok = Options.ValidKeys.Any(k => FixedTimeEquals(k, presentedKey));
        if (!ok)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "api-key"),
            new Claim(ClaimTypes.Name, "api-key"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        // Avoid early-out comparisons
        if (a is null || b is null) return false;

        var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
        var bBytes = System.Text.Encoding.UTF8.GetBytes(b);

        // Different lengths still do work (simple approach)
        var diff = aBytes.Length ^ bBytes.Length;
        var max = Math.Max(aBytes.Length, bBytes.Length);

        for (int i = 0; i < max; i++)
        {
            var av = i < aBytes.Length ? aBytes[i] : (byte)0;
            var bv = i < bBytes.Length ? bBytes[i] : (byte)0;
            diff |= av ^ bv;
        }

        return diff == 0;
    }
}
