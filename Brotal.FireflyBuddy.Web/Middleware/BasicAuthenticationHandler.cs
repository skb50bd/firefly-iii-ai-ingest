using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Brotal.FireflyBuddy.Web.Middleware;

public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    // Add any custom options here if needed
}

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSchemeOptions>
{
    private readonly Core.Models.Configuration.AuthenticationOptions _authOptions;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        Core.Models.Configuration.AuthenticationOptions authOptions)
        : base(options, logger, encoder)
    {
        _authOptions = authOptions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Task.CompletedTask;

        if (!_authOptions.Enabled)
        {
            // If authentication is disabled, create a default authenticated user
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "default-user"),
                new(ClaimTypes.Role, "Admin"),
                new(ClaimTypes.Role, "Ops"),
                new(ClaimTypes.Role, "JobsManager"),
                new(ClaimTypes.Role, "ApiUser")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Missing or invalid Authorization header");
        }

        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = credentials.IndexOf(':');

            if (separatorIndex == -1)
            {
                return AuthenticateResult.Fail("Invalid credentials format");
            }

            var username = credentials.Substring(0, separatorIndex);
            var password = credentials.Substring(separatorIndex + 1);

            // Validate against configured basic auth
            if (_authOptions.BasicAuth?.Username == username && 
                _authOptions.BasicAuth?.Password == password)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, username),
                    new(ClaimTypes.Role, "Admin"),
                    new(ClaimTypes.Role, "Ops"),
                    new(ClaimTypes.Role, "JobsManager"),
                    new(ClaimTypes.Role, "ApiUser")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }

            // Check API keys as well
            if (Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
            {
                var apiKey = apiKeyHeader.ToString();
                if (_authOptions.ApiKeys.Contains(apiKey))
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, "api-user"),
                        new(ClaimTypes.Role, "ApiUser"),
                        new(ClaimTypes.Role, "JobsManager")
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }
            }

            return AuthenticateResult.Fail("Invalid credentials");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during basic authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }
}
