using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel.Primitives;

/// <summary>
/// Step 1 — First API Call (Module 1: Foundry Platform Overview &amp; Setup)
///
/// Demonstrates the simplest possible interaction with a Foundry model
/// deployment: authenticate with Entra ID and call chat completions
/// via the OpenAI-compatible endpoint.
///
/// Portal equivalent: Playground → Chat → send a message.
/// Reference: https://learn.microsoft.com/azure/foundry/openai/how-to/chatgpt
/// </summary>
public static class Step01_FirstApiCall
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 1: First API Call (Module 1) ━━━");
        Console.WriteLine();

        // Read configuration
        var endpoint = config["FOUNDRY_PROJECT_ENDPOINT"]
            ?? config["Foundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException(
                "Set FOUNDRY_PROJECT_ENDPOINT or Foundry:ProjectEndpoint.");

        var modelDeployment = config["FOUNDRY_MODEL_DEPLOYMENT_NAME"]
            ?? config["Foundry:ModelDeploymentName"]
            ?? "gpt-4.1";

        // Build the OpenAI-compatible base URL from the Foundry project endpoint
        // Format: https://<resource>.services.ai.azure.com/openai/v1
        var baseUri = new Uri(endpoint);
        var openAiEndpoint = new Uri($"{baseUri.Scheme}://{baseUri.Host}/openai/v1");

        Console.WriteLine($"  Endpoint : {openAiEndpoint}");
        Console.WriteLine($"  Model    : {modelDeployment}");
        Console.WriteLine();

        // Authenticate with Entra ID (DefaultAzureCredential supports
        // managed identity, Azure CLI, VS Code, and more)
        var tokenPolicy = new BearerTokenPolicy(
            new DefaultAzureCredential(),
            "https://cognitiveservices.azure.com/.default");

        ChatClient chatClient = new(
            model: modelDeployment,
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions { Endpoint = openAiEndpoint });

        // Send a simple estimation question
        Console.WriteLine("  Sending chat completion request...");
        Console.WriteLine();

        ChatCompletion completion = await chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(
                "You are the Contoso Estimator Advisor, an AI assistant that helps "
                + "construction estimators prepare project bids. Be concise."),
            new UserChatMessage(
                "What are the typical cost components in a road construction estimate?"),
        ]);

        Console.WriteLine($"  Model response:");
        Console.WriteLine($"  {completion.Content[0].Text}");
        Console.WriteLine();
        Console.WriteLine($"  Tokens — prompt: {completion.Usage.InputTokenCount}, "
            + $"completion: {completion.Usage.OutputTokenCount}");
        Console.WriteLine();
        Console.WriteLine("✅ Step 1 complete — first API call succeeded.");
    }
}
