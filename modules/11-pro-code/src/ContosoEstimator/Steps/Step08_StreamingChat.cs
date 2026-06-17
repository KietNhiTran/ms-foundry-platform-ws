using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

/// <summary>
/// Step 8 — Full Streaming Chat UI (Bonus: Production Pattern)
///
/// Runs an interactive console-based chat loop using the Responses API
/// with streaming.  This demonstrates the production pattern for building
/// real-time conversational UIs with the Foundry SDK.
///
/// Features:
/// - Multi-turn conversation with memory (server-side conversation)
/// - Streaming token-by-token response display
/// - Graceful session management
///
/// Reference: https://learn.microsoft.com/azure/foundry/agents/concepts/runtime-components
/// </summary>
public static class Step08_StreamingChat
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 8: Streaming Chat UI (Bonus) ━━━");
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

        // Create the agent with all tools for the full experience
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
                + "Guidelines:\n"
                + "- Always cite source documents when referencing rates or policies\n"
                + "- Present costs in clear table format when possible\n"
                + "- NEVER disclose internal margin percentages or markup formulas\n"
                + "- All estimates are preliminary and require senior estimator review\n"
                + "- Be concise but thorough",

            Tools =
            {
                ResponseTool.CreateFileSearchTool(vectorStoreIds: Array.Empty<string>()),
                ResponseTool.CreateCodeInterpreterTool(container: null!),
            },
        };

        Console.WriteLine("  Creating agent...");

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent ready: {agentVersion.Name} v{agentVersion.Version}");
        Console.WriteLine();

        // Create a conversation for multi-turn chat
        ProjectConversation conversation = await projectClient.ProjectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync();

        // Get the responses client bound to this agent and conversation
        ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(
                defaultAgent: agentVersion.Name,
                defaultConversationId: conversation.Id);

        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Contoso Estimator Advisor — Streaming Chat        ║");
        Console.WriteLine("║   Type your estimation questions below.             ║");
        Console.WriteLine("║   Type 'quit' or 'exit' to end the session.         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  You > ");
            Console.ResetColor();

            string? userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("quit", StringComparison.OrdinalIgnoreCase)
                || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.WriteLine("  Ending session...");
                break;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  Agent > ");
            Console.ResetColor();

            // Stream the response token by token
            await foreach (StreamingResponseUpdate update
                in responsesClient.CreateResponseStreamingAsync(userInput))
            {
                if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
                {
                    Console.Write(textDelta.Delta);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        // Clean up
        Console.WriteLine("  Cleaning up agent version...");
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine();
        Console.WriteLine("✅ Step 8 complete — streaming chat session ended.");
    }
}
