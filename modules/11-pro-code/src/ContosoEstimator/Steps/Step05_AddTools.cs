using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

/// <summary>
/// Step 5 — Wire Additional Tools (Module 5: Foundry Toolkit)
///
/// Adds Code Interpreter (for cost calculations from BOQ quantities) and
/// an OpenAPI tool (for external pricing data) to the Contoso Estimator
/// agent, demonstrating multi-tool orchestration.
///
/// Portal equivalent: Agent → Tools → Add Code Interpreter → Add OpenAPI.
/// Reference: https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog
/// </summary>
public static class Step05_AddTools
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 5: Additional Tools (Module 5) ━━━");
        Console.WriteLine();

        var endpoint = config["FOUNDRY_PROJECT_ENDPOINT"]
            ?? config["Foundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException(
                "Set FOUNDRY_PROJECT_ENDPOINT or Foundry:ProjectEndpoint.");

        var modelDeployment = config["FOUNDRY_MODEL_DEPLOYMENT_NAME"]
            ?? config["Foundry:ModelDeploymentName"]
            ?? "gpt-4.1";

        var agentName = config["FOUNDRY_AGENT_NAME"]
            ?? config["Foundry:AgentName"]
            ?? "contoso-estimator-advisor";

        Console.WriteLine($"  Endpoint : {endpoint}");
        Console.WriteLine($"  Model    : {modelDeployment}");
        Console.WriteLine();

        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Build agent definition with multiple tools
        var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
        {
            Instructions =
                "You are the Contoso Estimator Advisor for Contoso Infrastructure.\n\n"
                + "You have access to the following tools:\n"
                + "1. **File Search** — rate libraries and estimation policies\n"
                + "2. **Code Interpreter** — perform cost calculations, generate charts, "
                + "analyze BOQ data\n"
                + "3. **Web Search** — look up current material prices and market data\n\n"
                + "When performing cost calculations:\n"
                + "- Show your work step by step\n"
                + "- Present results in a table format\n"
                + "- Include unit rates, quantities, and totals\n"
                + "- Apply appropriate contingency percentages",

            Tools =
            {
                // File Search — document retrieval from uploaded files
                ResponseTool.CreateFileSearchTool(vectorStoreIds: Array.Empty<string>()),

                // Code Interpreter — execute code for calculations and charts
                ResponseTool.CreateCodeInterpreterTool(container: null!),

                // Web Search — real-time market data lookup
                ResponseTool.CreateWebSearchTool(),
            },
        };

        Console.WriteLine($"  Creating agent with {agentDefinition.Tools.Count} tools:");
        Console.WriteLine($"    - File Search (rate libraries, policies)");
        Console.WriteLine($"    - Code Interpreter (cost calculations)");
        Console.WriteLine($"    - Web Search (market prices)");
        Console.WriteLine();

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent created: {agentVersion.Name} v{agentVersion.Version}");
        Console.WriteLine();

        // Test multi-tool orchestration with a cost calculation question
        Console.WriteLine("  Testing multi-tool orchestration (cost calculation)...");

        ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(agentVersion.Name);

        var response = await responsesClient.CreateResponseAsync(
            "Calculate the total cost for 500m of asphalt road resurfacing. "
            + "Assume: 6m wide, 50mm overlay depth, asphalt at $120/tonne, "
            + "density 2.4 tonnes/m³. Include labor and plant at 40% of materials.");

        Console.WriteLine($"  Agent response:");
        Console.WriteLine($"  {response.Value.GetOutputText()}");
        Console.WriteLine();

        // Clean up
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine("✅ Step 5 complete — Code Interpreter + Web Search tools added.");
    }
}
