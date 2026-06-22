/// <summary>
/// Bonus: Full Streaming Chat UI (Production Pattern)
/// No portal equivalent — this is how you'd build a production chat client.
///
/// Demonstrates:
/// - Conversation management (ProjectConversationsClient)
/// - True token-by-token streaming via CreateResponseStreamingAsync
/// - MCP approval handling (for Foundry IQ) in the streaming loop
/// - Interactive console chat with visible prompts (explicit flush so VS Code
///   integrated terminals / PowerShell extension host don't buffer the prompt)
/// </summary>

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
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

        // One conversation per session = multi-turn context retained server-side.
        var conversationClient = projectClient.ProjectOpenAIClient.GetProjectConversationsClient();
        ProjectConversation conversation = (await conversationClient.CreateProjectConversationAsync()).Value;
        Console.WriteLine($"Conversation:       {conversation.Id}");
        Console.WriteLine("Type your estimation questions. Type 'quit' or 'exit' to end.\n");

        var responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(
                defaultAgent: agentName,
                defaultConversationId: conversation.Id);

        while (true)
        {
            // Console.Write does NOT auto-flush in some hosts (VS Code PowerShell
            // extension terminal, redirected stdout). Explicit flush guarantees
            // the prompt is visible before ReadLine blocks for keystrokes.
            Console.Write("You: ");
            Console.Out.Flush();

            var input = Console.ReadLine();
            if (input is null) break; // stdin closed (Ctrl+Z / Ctrl+D)
            input = input.Trim();
            if (input.Length == 0) continue;
            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.Write("Assistant: ");
            Console.Out.Flush();

            // Build the first turn's request. The same options shape is reused
            // for any auto-approval round-trips (with PreviousResponseId set).
            var options = new CreateResponseOptions();
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(input));

            // The agent may pause to request MCP approval (Foundry IQ). Loop
            // a small bounded number of times to send approvals and resume.
            for (var iteration = 0; iteration < 4; iteration++)
            {
                ResponseResult? completed = await StreamResponseAsync(responsesClient, options);
                if (completed is null) break;

                var approval = completed.OutputItems
                    .OfType<McpToolCallApprovalRequestItem>()
                    .FirstOrDefault();

                if (approval is null) break;

                Console.WriteLine();
                Console.WriteLine($"[Auto-approving MCP call: {approval.ServerLabel}]");
                Console.Write("Assistant: ");
                Console.Out.Flush();

                options = new CreateResponseOptions { PreviousResponseId = completed.Id };
                options.InputItems.Add(
                    ResponseItem.CreateMcpApprovalResponseItem(
                        approvalRequestId: approval.Id,
                        approved: true));
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        Console.WriteLine("✅ Chat session ended.");
    }

    // Streams one Responses API call, writing each text delta to stdout as it
    // arrives. Returns the final ResponseResult (used to inspect OutputItems for
    // MCP approval requests) or null if the stream produced no completed update.
    private static async Task<ResponseResult?> StreamResponseAsync(
        ProjectResponsesClient client,
        CreateResponseOptions options)
    {
        ResponseResult? completed = null;

        await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(options))
        {
            switch (update)
            {
                case StreamingResponseOutputTextDeltaUpdate delta:
                    Console.Write(delta.Delta);
                    Console.Out.Flush();
                    break;

                case StreamingResponseCompletedUpdate done:
                    completed = done.Response;
                    break;
            }
        }

        return completed;
    }
}
