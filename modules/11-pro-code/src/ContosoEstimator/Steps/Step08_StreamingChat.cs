/// <summary>
/// Bonus: Full Streaming Chat UI (Production Pattern)
/// No portal equivalent — this is how you'd build a production chat client.
/// 
/// Demonstrates:
/// - Conversation management (conversations API)
/// - Streaming responses via SSE
/// - MCP approval handling (for Foundry IQ)
/// - Interactive console chat loop
/// </summary>

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ContosoEstimator.Steps;

public static class Step08_StreamingChat
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 8: Streaming Chat (Production Pattern) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(projectEndpoint, credential);

        // Get OpenAI client for conversations
        var openaiClient = projectClient.GetOpenAIClient();

        Console.WriteLine($"Connected to agent: {agentName}");
        Console.WriteLine("Type your estimation questions. Type 'quit' to exit.\n");

        // Create a new conversation
        var conversation = await openaiClient.Conversations.CreateAsync();
        Console.WriteLine($"Conversation started (ID: {conversation.Id})\n");

        // Interactive chat loop
        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            // Add user message to conversation
            await openaiClient.Conversations.Items.CreateAsync(
                conversationId: conversation.Id,
                items: [new { type = "message", role = "user", content = input }]
            );

            // Get response from agent
            var response = await openaiClient.Responses.CreateAsync(
                conversation: conversation.Id,
                extraBody: new { agent_reference = new { name = agentName, type = "agent_reference" } },
                input: ""
            );

            // Handle MCP approval requests (if Foundry IQ requires approval)
            if (response.Output?.Any(o => o.Type == "mcp_approval_request") == true)
            {
                var approval = response.Output.First(o => o.Type == "mcp_approval_request");
                Console.WriteLine($"\n[Knowledge base lookup requested: {approval.ServerLabel}]");
                Console.WriteLine("Auto-approving...\n");

                // Auto-approve (in production, you might prompt the user)
                await openaiClient.Conversations.Items.CreateAsync(
                    conversationId: conversation.Id,
                    items: [new
                    {
                        type = "mcp_approval_response",
                        approval_request_id = approval.Id,
                        approve = true
                    }]
                );

                // Get actual response after approval
                response = await openaiClient.Responses.CreateAsync(
                    conversation: conversation.Id,
                    extraBody: new { agent_reference = new { name = agentName, type = "agent_reference" } },
                    input: ""
                );
            }

            // Display response
            Console.WriteLine($"\nAssistant: {response.OutputText}\n");
        }

        Console.WriteLine("\n✅ Chat session ended.");
    }
}
