using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using Brotal.FireflyBuddy.Core.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Brotal.FireflyBuddy.Core.Services;

public static class AiExtensions
{
    public static IServiceCollection AddAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var aiProvider = configuration.GetValue<string>("AiProvider");

        if (aiProvider?.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var openAiOptions =
                configuration.GetSection("OpenAI").Get<OpenAiOptions>()
                    ?? throw new InvalidOperationException("OpenAI configuration is missing.");

            if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Please set the 'OpenAI:ApiKey' configuration value."
                );
            }

            if (string.IsNullOrWhiteSpace(openAiOptions.Model))
            {
                throw new InvalidOperationException(
                    "OpenAI model is not configured. Please set the 'OpenAI:Model' configuration value."
                );
            }

            var chatClient =
                new OpenAIClient(openAiOptions.ApiKey)
                    .GetChatClient(openAiOptions.Model)
                    .AsIChatClient();

            services
                .AddSingleton(openAiOptions)
                .AddSingleton(chatClient);
        }
        else if (aiProvider?.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var ollamaOptions =
            configuration.GetSection("Ollama").Get<OllamaOptions>()
                ?? throw new InvalidOperationException("Ollama configuration is missing.");

            if (string.IsNullOrWhiteSpace(ollamaOptions.Model))
            {
                throw new InvalidOperationException("Ollama model is not configured. Please set the 'Ollama:Model' configuration value.");
            }

            if (string.IsNullOrWhiteSpace(ollamaOptions.Url))
            {
                throw new InvalidOperationException("Ollama Url is not configured. Please set the 'Ollama:Uri' configuration value.");
            }

            var chatClient =
                new OllamaApiClient(new Uri(ollamaOptions.Url), ollamaOptions.Model);

            services
                .AddSingleton(ollamaOptions)
                .AddSingleton<IChatClient>(chatClient);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers are: OpenAI, Ollama");
        }

        return services;
    }
}
