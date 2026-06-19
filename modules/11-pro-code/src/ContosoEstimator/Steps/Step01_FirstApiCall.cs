/// <summary>
/// Module 1: Foundry Platform Overview & Setup (Pro-Code Version)
/// Portal equivalent: Playground → send a chat completion message
/// 
/// This step demonstrates calling a model via the Foundry project endpoint
/// using the Responses API from the Azure AI Projects SDK.
/// </summary>

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

#pragma warning disable OPENAI001

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

        // Create project client pointing to Foundry endpoint
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        // Use the Responses API via ProjectOpenAIClient
        var responseClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForModel(modelDeployment);

        // Send a simple prompt
        var result = await responseClient.CreateResponseAsync(
            "You are a helpful construction estimation assistant. " +
            "What is a bill of quantities (BOQ) in construction?");

        Console.WriteLine($"Model: {modelDeployment}");
        Console.WriteLine($"\nResponse:\n{result.Value.GetOutputText()}");
        Console.WriteLine("\n✅ Step 1 complete — model is responding.\n");
    }
}
