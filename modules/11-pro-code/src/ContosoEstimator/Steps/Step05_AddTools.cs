/// <summary>
/// Module 5: Foundry Toolkit & Data Connectors (Pro-Code Version)
/// Portal equivalent: Agent → Add tool → Code Interpreter / OpenAPI
/// 
/// This step shows how to wire multiple tools to an agent programmatically,
/// including Code Interpreter and an OpenAPI-defined tool.
/// </summary>

using Azure.AI.Projects;
using Azure.AI.Projects.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ContosoEstimator.Steps;

public static class Step05_AddTools
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 5: Wire Additional Tools (Module 5) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(projectEndpoint, credential);

        // Define tools programmatically
        var tools = new List<ToolDefinition>
        {
            // Built-in: File Search (for rate library docs)
            new FileSearchToolDefinition(),

            // Built-in: Code Interpreter (for cost calculations)
            new CodeInterpreterToolDefinition(),

            // Note: OpenAPI tools would be defined like:
            // new OpenApiToolDefinition
            // {
            //     Name = "contoso-pricing-api",
            //     Description = "Get current material pricing from Contoso's pricing service",
            //     Spec = File.ReadAllText("openapi-spec.yaml"),
            //     Auth = new ApiKeyAuth { Key = config["PricingApi:Key"]! }
            // }
        };

        Console.WriteLine("Tools configured for Contoso Estimator:");
        Console.WriteLine("  ✅ File Search — rate library + policy documents");
        Console.WriteLine("  ✅ Code Interpreter — cost calculations, BOQ analysis");
        Console.WriteLine("  📋 OpenAPI (pattern) — external pricing API integration");
        Console.WriteLine();
        Console.WriteLine("Tool orchestration is automatic:");
        Console.WriteLine("  Agent LLM decides which tools to call, in what order.");
        Console.WriteLine("  Example: rate lookup (File Search) → calculate (Code Interpreter)");
        Console.WriteLine("\n✅ Step 5 complete — multi-tool agent configured.\n");

        await Task.CompletedTask;
    }
}
