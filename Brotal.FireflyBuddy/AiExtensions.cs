using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace Brotal.FireflyBuddy;

public static class AiExtensions
{
    public static WebApplicationBuilder AddAiChatClient(this WebApplicationBuilder builder)
    {
        var aiProvider = builder.Configuration.GetValue<string>("AiProvider");

        if (aiProvider?.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var openAiOptions =
                builder.Configuration.GetSection("OpenAI").Get<OpenAiOptions>()
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

            builder.Services
                .AddSingleton(openAiOptions)
                .AddSingleton(chatClient);
        }
        else if (aiProvider?.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var ollamaOptions =
            builder.Configuration.GetSection("Ollama").Get<OllamaOptions>()
                ?? throw new InvalidOperationException("Ollama configuration is missing.");

            if (string.IsNullOrWhiteSpace(ollamaOptions.ApiKey))
            {
                throw new InvalidOperationException("Ollama API key is not configured. Please set the 'Ollama:ApiKey' configuration value.");
            }

            if (string.IsNullOrWhiteSpace(ollamaOptions.Model))
            {
                throw new InvalidOperationException("Ollama model is not configured. Please set the 'Ollama:Model' configuration value.");
            }

            if (string.IsNullOrWhiteSpace(ollamaOptions.Url))
            {
                throw new InvalidOperationException("Ollama Url is not configured. Please set the 'Ollama:Url' configuration value.");
            }

            var chatClient =
                new OllamaApiClient(new Uri(ollamaOptions.Url), ollamaOptions.Model);

            builder.Services
                .AddSingleton(ollamaOptions)
                .AddSingleton<IChatClient>(chatClient);
        }
        else if (aiProvider?.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var geminiOptions =
                builder.Configuration.GetSection("Gemini").Get<GeminiClientOptions>()
                    ?? throw new InvalidOperationException("Gemini configuration is missing.");

            if (string.IsNullOrWhiteSpace(geminiOptions.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured. Please set the 'Gemini:ApiKey' configuration value.");
            }

            var geminiClient = new GeminiClient(geminiOptions);
            var chatClient = new GeminiChatClient(geminiOptions);

            builder.Services
                .AddSingleton(geminiOptions)
                .AddSingleton(geminiClient)
                .AddSingleton<IChatClient>(chatClient);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported AI provider: {aiProvider}. Supported providers are: OpenAI, Ollama, Gemini");
        }

        return builder;
    }
}
