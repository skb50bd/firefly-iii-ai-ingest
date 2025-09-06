using System.ComponentModel.DataAnnotations;

namespace Brotal.FireflyBuddy.Core.Models.Configuration;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-mini";
}

public sealed class OllamaOptions
{
    public string Url { get; set; } = string.Empty;
    public string Model { get; set; } = "gemma3:1b";
}

public sealed class FireflyOptions
{
    public string Url { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
}

public sealed class AuthenticationOptions
{
    public bool Enabled { get; set; } = false;
    public string[] ApiKeys { get; set; } = [];
    public BasicAuthOptions? BasicAuth { get; set; }
    public TickerQAuthOptions? TickerQ { get; set; }
}

public sealed class BasicAuthOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class TickerQAuthOptions
{
    public bool Enabled { get; set; } = true;
    public string[] RequiredRoles { get; set; } = ["Admin", "Ops"];
    public string[] RequiredPolicies { get; set; } = ["JobsDashboardAccess"];
    public BasicAuthOptions? BasicAuth { get; set; }
    public bool UseHostAuthentication { get; set; } = true;
}
