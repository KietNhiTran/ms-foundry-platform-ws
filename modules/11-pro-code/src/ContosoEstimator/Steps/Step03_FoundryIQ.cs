/// <summary>
/// Module 3: Foundry IQ & Agentic RAG (Pro-Code Version)
/// Portal equivalent: Agent → Knowledge → Connect to Foundry IQ
/// 
/// This step programmatically creates an MCP connection to a Foundry IQ
/// knowledge base and adds it as a tool to the agent.
/// 
/// NOTE: As of June 2026, the Foundry IQ integration is officially supported
/// only via the Python SDK and REST API (C# SDK shows "-" in the support matrix).
/// However, since Foundry IQ exposes a standard MCP endpoint, we can use the
/// generic C# MCP tool support (ResponseTool.CreateMcpTool) to connect to it.
/// The project connection (RemoteTool) must be created via REST API or Python first.
/// See: https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect
/// </summary>

using System.Net.Http.Headers;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step03_FoundryIQ
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 3: Connect Foundry IQ Knowledge Base (Module 3) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;
        var searchEndpoint = config["FoundryIQ:SearchEndpoint"]!;
        var knowledgeBaseName = config["FoundryIQ:KnowledgeBaseName"]!;
        var connectionName = config["FoundryIQ:ConnectionName"]!;
        var projectResourceId = config["Foundry:ProjectResourceId"]!;

        var credential = new DefaultAzureCredential();

        // The MCP endpoint for the knowledge base
        var mcpEndpoint = $"{searchEndpoint}/knowledgebases/{knowledgeBaseName}/mcp?api-version=2026-05-01-preview";

        Console.WriteLine($"Search endpoint: {searchEndpoint}");
        Console.WriteLine($"Knowledge base: {knowledgeBaseName}");
        Console.WriteLine($"MCP endpoint: {mcpEndpoint}");

        // Step 1: Create the RemoteTool project connection via REST API
        // (C# SDK does not have a dedicated Foundry IQ helper — use ARM REST)
        Console.WriteLine("\n--- Creating project connection (RemoteTool) via REST API ---");
        await CreateProjectConnectionAsync(credential, projectResourceId, connectionName, mcpEndpoint);

        // Step 2: Create agent with MCP tool using the correct C# SDK API
        Console.WriteLine("\n--- Creating agent with MCP tool ---");
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        // Create the MCP tool using ResponseTool.CreateMcpTool (correct C# API)
        var mcpKbTool = ResponseTool.CreateMcpTool(
            serverLabel: "knowledge-base",
            serverUri: new Uri(mcpEndpoint),
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(
                GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
        mcpKbTool.ProjectConnectionId = connectionName;

        var instructions = """
            You are the Contoso Estimator Advisor.
            
            Use the knowledge base tool to answer questions about historical project data.
            If the knowledge base doesn't contain the answer, say "I don't have that information
            in the project history database."
            
            Always include citations to retrieved sources.
            """;

        // Use DeclarativeAgentDefinition (the correct C# type for prompt agents)
        var agentDefinition = new DeclarativeAgentDefinition(model: modelDeployment)
        {
            Instructions = instructions,
            Tools = { mcpKbTool }
        };

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: "contoso-estimator-advisor",
            options: new(agentDefinition));

        Console.WriteLine($"\nAgent created with Foundry IQ tool: {agentVersion.Name} (version: {agentVersion.Version})");
        Console.WriteLine("Tools: knowledge_base_retrieve (MCP via Foundry IQ)");
        Console.WriteLine("\n✅ Step 3 complete — Foundry IQ connected via MCP tool.\n");
    }

    /// <summary>
    /// Creates a RemoteTool project connection targeting the Foundry IQ MCP endpoint.
    /// This uses the ARM REST API because the C# SDK does not yet support Foundry IQ connections natively.
    /// </summary>
    private static async Task CreateProjectConnectionAsync(
        DefaultAzureCredential credential,
        string projectResourceId,
        string connectionName,
        string mcpEndpoint)
    {
        var tokenRequestContext = new Azure.Core.TokenRequestContext(["https://management.azure.com/.default"]);
        var token = await credential.GetTokenAsync(tokenRequestContext);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var requestBody = new
        {
            name = connectionName,
            type = "Microsoft.MachineLearningServices/workspaces/connections",
            properties = new
            {
                authType = "ProjectManagedIdentity",
                category = "RemoteTool",
                target = mcpEndpoint,
                isSharedToAll = true,
                audience = "https://search.azure.com/",
                metadata = new { ApiType = "Azure" }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PutAsync(
            $"https://management.azure.com{projectResourceId}/connections/{connectionName}?api-version=2025-10-01-preview",
            content);

        response.EnsureSuccessStatusCode();
        Console.WriteLine($"Connection '{connectionName}' created or updated successfully.");
    }
}
