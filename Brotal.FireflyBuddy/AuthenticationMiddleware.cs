using System.Security.Claims;
using System.Text;

namespace Brotal.FireflyBuddy;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthenticationOptions _authOptions;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        RequestDelegate next,
        AuthenticationOptions authOptions,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _authOptions = authOptions;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_authOptions.Enabled)
        {
            await _next(context);
            return;
        }

        var isAuthenticated = false;

        // Check for API Key authentication
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            if (_authOptions.ApiKeys.Contains(apiKey))
            {
                isAuthenticated = true;
                _logger.LogDebug("API Key authentication successful");
            }
        }

        // Check for Basic authentication
        if (!isAuthenticated && context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                isAuthenticated = ValidateBasicAuth(authValue);
                if (isAuthenticated)
                {
                    _logger.LogDebug("Basic authentication successful");
                }
            }
        }

        if (!isAuthenticated)
        {
            _logger.LogWarning("Authentication failed for request to {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"FireflyBuddy API\"");
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        // Add claims to the context for future use if needed
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "authenticated-user"),
            new(ClaimTypes.Role, "api-user")
        };

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }

    private bool ValidateBasicAuth(string authHeader)
    {
        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = credentials.IndexOf(':');

            if (separatorIndex == -1) return false;

            var username = credentials.Substring(0, separatorIndex);
            var password = credentials.Substring(separatorIndex + 1);

            return _authOptions.BasicAuth?.Username == username &&
                   _authOptions.BasicAuth?.Password == password;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Basic authentication");
            return false;
        }
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
