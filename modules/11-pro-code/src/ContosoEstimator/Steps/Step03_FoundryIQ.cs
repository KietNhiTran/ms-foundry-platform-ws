using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

/// <summary>
/// Step 3 — Connect Foundry IQ Knowledge Base (Module 3: Agentic RAG)
///
/// Adds an MCP (Model Context Protocol) tool to the agent that connects to a
/// Foundry IQ knowledge base.  This enables enterprise-grade retrieval over
/// project history data stored in Blob Storage, with automated indexing,
/// citation, and permission enforcement.
///
/// Portal equivalent: Agent → Tools → Add Foundry IQ knowledge base.
/// Reference: https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq
/// </summary>
public static class Step03_FoundryIQ
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 3: Connect Foundry IQ (Module 3) ━━━");
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

        var foundryIqEndpoint = config["FOUNDRY_IQ_ENDPOINT"]
            ?? throw new InvalidOperationException(
                "Set FOUNDRY_IQ_ENDPOINT to your Foundry IQ knowledge base endpoint.");

        Console.WriteLine($"  Endpoint      : {endpoint}");
        Console.WriteLine($"  Foundry IQ    : {foundryIqEndpoint}");
        Console.WriteLine();

        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Create an MCP tool pointing to the Foundry IQ knowledge base.
        // Behind the scenes, this is what the portal does when you click
        // "Add Foundry IQ" on the agent tools page.
        var mcpTool = ResponseTool.CreateMcpTool(
            serverLabel: "contoso-project-history",
            serverUri: new Uri(foundryIqEndpoint),
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(
                GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

        // Build agent definition with File Search + MCP (Foundry IQ)
        var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
        {
            Instructions =
                "You are the Contoso Estimator Advisor for Contoso Infrastructure.\n\n"
                + "You have access to:\n"
                + "1. File Search — rate libraries and estimation policies\n"
                + "2. Foundry IQ — historical project data (past bids, final costs, "
                + "lessons learned) from Blob Storage\n\n"
                + "When answering questions about past projects, use the Foundry IQ tool "
                + "to search historical data. Always cite sources.",

            Tools =
            {
                ResponseTool.CreateFileSearchTool(vectorStoreIds: Array.Empty<string>()),
                mcpTool,
            },
        };

        Console.WriteLine("  Creating agent with Foundry IQ connection...");

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent created with {agentDefinition.Tools.Count} tools:");
        Console.WriteLine($"    - File Search (rate libraries, policies)");
        Console.WriteLine($"    - MCP / Foundry IQ (project history)");
        Console.WriteLine($"    Name    : {agentVersion.Name}");
        Console.WriteLine($"    Version : {agentVersion.Version}");
        Console.WriteLine();

        // Test cross-document retrieval
        Console.WriteLine("  Testing cross-document retrieval...");

        ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(agentVersion.Name);

        var response = await responsesClient.CreateResponseAsync(
            "Compare earthworks rates across our last 3 road projects.");

        Console.WriteLine($"  Agent response:");
        Console.WriteLine($"  {response.Value.GetOutputText()}");
        Console.WriteLine();

        // Clean up
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine("✅ Step 3 complete — Foundry IQ knowledge base connected.");
    }
}
