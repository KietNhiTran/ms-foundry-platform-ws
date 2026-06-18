/// <summary>
/// Module 3: Foundry IQ & Agentic RAG (Pro-Code Version)
/// Portal equivalent: Agent → Knowledge → Connect to Foundry IQ
/// 
/// This step programmatically creates an MCP connection to a Foundry IQ
/// knowledge base and adds it as a tool to the agent.
/// </summary>

using Azure.AI.Projects;
using Azure.AI.Projects.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

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

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(projectEndpoint, credential);

        // The MCP endpoint for the knowledge base
        var mcpEndpoint = $"{searchEndpoint}/knowledgebases/{knowledgeBaseName}/mcp?api-version=2026-05-01-preview";

        Console.WriteLine($"Search endpoint: {searchEndpoint}");
        Console.WriteLine($"Knowledge base: {knowledgeBaseName}");
        Console.WriteLine($"MCP endpoint: {mcpEndpoint}");

        // Create the MCPTool — this is what the portal does behind the scenes
        // when you click "Connect to Foundry IQ"
        var mcpKbTool = new MCPTool
        {
            ServerLabel = "knowledge-base",
            ServerUrl = mcpEndpoint,
            RequireApproval = "never",
            AllowedTools = { "knowledge_base_retrieve" },
            ProjectConnectionId = connectionName
        };

        // Create/update agent with the KB tool added
        var instructions = """
            You are the Contoso Estimator Advisor.
            
            Use the knowledge base tool to answer questions about historical project data.
            If the knowledge base doesn't contain the answer, say "I don't have that information
            in the project history database."
            
            Always include citations to retrieved sources.
            """;

        var agent = await projectClient.Agents.CreateVersionAsync(
            agentName: "contoso-estimator-advisor",
            definition: new PromptAgentDefinition
            {
                Model = modelDeployment,
                Instructions = instructions,
                Tools =
                {
                    new FileSearchToolDefinition(),
                    new CodeInterpreterToolDefinition(),
                    mcpKbTool
                }
            }
        );

        Console.WriteLine($"\nAgent updated with Foundry IQ tool: {agent.Value.Name}");
        Console.WriteLine("Tools: File Search, Code Interpreter, knowledge_base_retrieve (MCP)");
        Console.WriteLine("\n✅ Step 3 complete — Foundry IQ connected via MCPTool.\n");
    }
}
