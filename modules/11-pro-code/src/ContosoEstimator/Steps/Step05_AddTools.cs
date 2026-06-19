/// <summary>
/// Module 5: Foundry Toolkit & Data Connectors (Pro-Code Version)
/// Portal equivalent: Agent → Add tool → Code Interpreter / OpenAPI
/// 
/// This step shows how to wire multiple tools to an agent programmatically,
/// including Code Interpreter and an OpenAPI-defined tool.
/// </summary>

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step05_AddTools
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 5: Wire Additional Tools (Module 5) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        // Define agent with multiple tools
        var agentDefinition = new DeclarativeAgentDefinition(model: modelDeployment)
        {
            Instructions = "You are the Contoso Estimator Advisor with multi-tool capabilities.",
            Tools =
            {
                // Built-in: File Search (for rate library docs)
                ResponseTool.CreateFileSearchTool(vectorStoreIds: []),

                // Built-in: Code Interpreter (for cost calculations)
                ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(
                        new AutomaticCodeInterpreterToolContainerConfiguration())),

                // Note: MCP tools (e.g., OpenAPI endpoints) can be added via:
                // ResponseTool.CreateMcpTool(
                //     serverLabel: "contoso-pricing-api",
                //     serverUri: new Uri("https://pricing.contoso.com/mcp"),
                //     toolCallApprovalPolicy: new McpToolCallApprovalPolicy(
                //         GlobalMcpToolCallApprovalPolicy.NeverRequireApproval))
            }
        };

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: "contoso-estimator-advisor",
            options: new(agentDefinition));

        Console.WriteLine($"Agent: {agentVersion.Name} (version: {agentVersion.Version})");
        Console.WriteLine("Tools configured for Contoso Estimator:");
        Console.WriteLine("  ✅ File Search — rate library + policy documents");
        Console.WriteLine("  ✅ Code Interpreter — cost calculations, BOQ analysis");
        Console.WriteLine("  📋 MCP (pattern) — external API integration via MCP endpoint");
        Console.WriteLine();
        Console.WriteLine("Tool orchestration is automatic:");
        Console.WriteLine("  Agent LLM decides which tools to call, in what order.");
        Console.WriteLine("  Example: rate lookup (File Search) → calculate (Code Interpreter)");
        Console.WriteLine("\n✅ Step 5 complete — multi-tool agent configured.\n");
    }
}
