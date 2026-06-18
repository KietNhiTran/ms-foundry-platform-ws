/// <summary>
/// Module 1: Foundry Platform Overview & Setup (Pro-Code Version)
/// Portal equivalent: Playground → send a chat completion message
/// 
/// This step demonstrates calling GPT-4.1 via the OpenAI-compatible endpoint
/// using the standard OpenAI client with a Foundry base URL.
/// </summary>

using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace ContosoEstimator.Steps;

public static class Step01_FirstApiCall
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 1: First API Call (Module 1) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;

        // Authenticate with Entra ID (recommended over API keys)
        var credential = new DefaultAzureCredential();

        // Create OpenAI client pointing to Foundry endpoint
        var client = new OpenAIClient(
            credential,
            new OpenAIClientOptions { Endpoint = new Uri($"{projectEndpoint}/openai/v1") }
        );

        var chatClient = client.GetChatClient(modelDeployment);

        // Send a simple chat completion
        var response = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage("You are a helpful construction estimation assistant."),
                new UserChatMessage("What is a bill of quantities (BOQ) in construction?")
            ]
        );

        Console.WriteLine($"Model: {modelDeployment}");
        Console.WriteLine($"Tokens: {response.Value.Usage.TotalTokenCount}");
        Console.WriteLine($"\nResponse:\n{response.Value.Content[0].Text}");
        Console.WriteLine("\n✅ Step 1 complete — model is responding.\n");
    }
}
