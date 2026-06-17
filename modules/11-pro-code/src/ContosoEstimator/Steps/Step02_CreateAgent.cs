using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

/// <summary>
/// Step 2 — Create Agent with File Search (Module 2: Build Your First Agent)
///
/// Creates the Contoso Estimator Advisor agent programmatically using the
/// Azure.AI.Projects SDK.  Adds a File Search tool so the agent can retrieve
/// information from uploaded rate-library and estimation-policy documents.
///
/// Portal equivalent: Agent Service → Create Agent → add File Search tool → upload files.
/// Reference: https://learn.microsoft.com/azure/foundry/agents/quickstarts/prompt-agent
/// </summary>
public static class Step02_CreateAgent
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 2: Create Agent + File Search (Module 2) ━━━");
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
        Console.WriteLine($"  Agent    : {agentName}");
        Console.WriteLine();

        // Create the Foundry project client
        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Define the agent with system instructions for estimation
        var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
        {
            Instructions =
                "You are the Contoso Estimator Advisor for Contoso Infrastructure, "
                + "a large-scale construction and engineering company.\n\n"
                + "Your role:\n"
                + "- Search historical project data (past bids, final costs, lessons learned)\n"
                + "- Look up rate libraries (labor, plant, materials by region)\n"
                + "- Reference company policies (margin guidelines, approval thresholds)\n"
                + "- Perform cost calculations (preliminary estimates from BOQ quantities)\n\n"
                + "Always cite the source document when referencing rates or policies. "
                + "Present costs in a clear table format when possible.",

            // Add the File Search tool for document retrieval
            Tools = { ResponseTool.CreateFileSearchTool(vectorStoreIds: Array.Empty<string>()) },
        };

        Console.WriteLine("  Creating agent version...");

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent created:");
        Console.WriteLine($"    Name    : {agentVersion.Name}");
        Console.WriteLine($"    Version : {agentVersion.Version}");
        Console.WriteLine($"    Id      : {agentVersion.Id}");
        Console.WriteLine();

        // Test the agent with a sample question
        Console.WriteLine("  Testing agent with a sample question...");

        ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(agentVersion.Name);

        var response = await responsesClient.CreateResponseAsync(
            "What labor rate categories are typically used in road construction estimates?");

        Console.WriteLine($"  Agent response:");
        Console.WriteLine($"  {response.Value.GetOutputText()}");
        Console.WriteLine();

        // Clean up — delete the agent version (optional in demo)
        Console.WriteLine("  Cleaning up agent version...");
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine("✅ Step 2 complete — agent created with File Search tool.");
    }
}
