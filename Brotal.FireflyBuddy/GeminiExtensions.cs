using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using GeminiDotnet;
using GeminiDotnet.ContentGeneration;

namespace Brotal.FireflyBuddy;

public static class GeminiExtensions
{
    public async static Task<T> GetGeminiResponse<T>(
        this IServiceProvider serviceProvider, 
        string systemPrompt,
        string message, 
        CancellationToken cancellationToken
    )
    {
        var geminiOptions = serviceProvider.GetService<GeminiClientOptions>();
        var client = serviceProvider.GetService<GeminiClient>();
        var exporterOptions = new JsonSchemaExporterOptions();
        var serializerOptions = JsonSerializerOptions.Default;
        var schema = serializerOptions.GetJsonSchemaAsNode(typeof(T), exporterOptions);

        var request = new GenerateContentRequest()
        {
            Contents = [
            ],
            GenerationConfiguration = null,
            SystemInstruction = new Content { Parts = [] }
        };
        
        var content = await client.GenerateContentAsync(
            geminiOptions?.ModelId ?? "gemini-2.5-flash-lite",
            client.
            message, 
            cancellationToken: cancellationToken
        );

        return (T)new object();
    }
}