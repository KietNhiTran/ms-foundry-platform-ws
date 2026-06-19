/// <summary>
/// Bonus: Full Streaming Chat UI (Production Pattern)
/// No portal equivalent — this is how you'd build a production chat client.
/// 
/// Demonstrates:
/// - Conversation management (ProjectConversationsClient)
/// - Agent responses via Responses API
/// - MCP approval handling (for Foundry IQ)
/// - Interactive console chat loop
/// </summary>

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step08_StreamingChat
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 8: Streaming Chat (Production Pattern) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        Console.WriteLine($"Connected to agent: {agentName}");
        Console.WriteLine("Type your estimation questions. Type 'quit' to exit.\n");

        // Create a new conversation for multi-turn context
        var conversationClient = projectClient.ProjectOpenAIClient
            .GetProjectConversationsClient();
        ProjectConversation conversation = (await conversationClient.CreateProjectConversationAsync()).Value;
        Console.WriteLine($"Conversation started (ID: {conversation.Id})\n");

        // Get responses client scoped to this agent and conversation
        var responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(
                defaultAgent: agentName,
                defaultConversationId: conversation.Id);

        // Interactive chat loop
        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            // Send user message and get response from agent
            var responseOptions = new CreateResponseOptions(
                input,
                [ResponseItem.CreateUserMessageItem(input)]);

            var response = (await responsesClient.CreateResponseAsync(responseOptions)).Value;

            // Handle MCP approval requests (if Foundry IQ requires approval)
            CreateResponseOptions? nextOptions = null;
            foreach (ResponseItem item in response.OutputItems)
            {
                if (item is McpToolCallApprovalRequestItem mcpApproval)
                {
                    Console.WriteLine($"\n[Knowledge base lookup requested: {mcpApproval.ServerLabel}]");
                    Console.WriteLine("Auto-approving...\n");

                    // Auto-approve (in production, you might prompt the user)
                    nextOptions = new CreateResponseOptions()
                    {
                        PreviousResponseId = response.Id,
                    };
                    nextOptions.InputItems.Add(
                        ResponseItem.CreateMcpApprovalResponseItem(
                            approvalRequestId: mcpApproval.Id,
                            approved: true));
                }
            }

            if (nextOptions is not null)
            {
                // Get actual response after approval
                response = (await responsesClient.CreateResponseAsync(nextOptions)).Value;
            }

            // Display response
            Console.WriteLine($"\nAssistant: {response.GetOutputText()}\n");
        }

        Console.WriteLine("\n✅ Chat session ended.");
    }
}
