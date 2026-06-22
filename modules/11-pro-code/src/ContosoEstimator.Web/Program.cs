/// <summary>
/// Contoso Estimator — Streaming Chat Web UI (ASP.NET Core Minimal API)
///
/// A thin API layer between a browser-based chat UI and Foundry Agent Service.
/// Follows the Basic Microsoft Foundry Chat reference architecture:
///   https://learn.microsoft.com/azure/architecture/ai-ml/architecture/basic-microsoft-foundry-chat
///
/// The backend:
///   1. Authenticates to Foundry using DefaultAzureCredential
///   2. Manages Foundry conversations (create / delete)
///   3. Proxies chat messages to the Foundry agent and streams responses via SSE
///   4. Handles MCP approval requests (auto-approve for demo)
///
/// Run: dotnet run
/// Browse: http://localhost:5050
/// </summary>

using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

// Configuration from appsettings.json or environment variables
var projectEndpoint = builder.Configuration["Foundry:ProjectEndpoint"]
    ?? Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException(
        "Set Foundry:ProjectEndpoint in appsettings.json or AZURE_AI_PROJECT_ENDPOINT env var.");

var agentName = builder.Configuration["Foundry:AgentName"]
    ?? Environment.GetEnvironmentVariable("AZURE_AI_AGENT_NAME")
    ?? "contoso-estimator-advisor";

var app = builder.Build();

// Foundry SDK clients
var credential = new DefaultAzureCredential();
var projectClient = new AIProjectClient(
    endpoint: new Uri(projectEndpoint),
    tokenProvider: credential);
var conversationsClient = projectClient.ProjectOpenAIClient.GetProjectConversationsClient();

// Serve static files from wwwroot/
app.UseStaticFiles();

// ── Health check ─────────────────────────────────────────────────────────────
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    agent_name = agentName,
    project_endpoint_configured = !string.IsNullOrEmpty(projectEndpoint),
}));

// ── Create conversation ──────────────────────────────────────────────────────
app.MapPost("/api/conversations", async () =>
{
    var conversation = await conversationsClient.CreateProjectConversationAsync();
    return Results.Ok(new { conversation_id = conversation.Value.Id });
});

// ── Chat — streaming via SSE ─────────────────────────────────────────────────
app.MapPost("/api/chat", async (HttpContext context, [FromBody] ChatRequest req) =>
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";

    var conversationId = req.ConversationId;

    // Create conversation if not provided
    if (string.IsNullOrEmpty(conversationId))
    {
        var conversation = await conversationsClient.CreateProjectConversationAsync();
        conversationId = conversation.Value.Id;
    }

    // Send conversation_id event so the client can persist it for follow-ups
    await WriteSseEvent(context, "conversation_id",
        new { conversation_id = conversationId });

    try
    {
        var responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(
                defaultAgent: agentName,
                defaultConversationId: conversationId);

        // Build the first turn's request. The same options shape is reused for
        // any MCP approval round-trips (with PreviousResponseId set).
        var options = new CreateResponseOptions();
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(req.Message));

        // Bounded loop to handle agent-initiated MCP approval pauses.
        for (var iteration = 0; iteration < 4; iteration++)
        {
            ResponseResult? completed = null;

            await foreach (StreamingResponseUpdate update in
                responsesClient.CreateResponseStreamingAsync(options))
            {
                switch (update)
                {
                    case StreamingResponseOutputTextDeltaUpdate delta:
                        await WriteSseEvent(context, "delta",
                            new { text = delta.Delta });
                        break;

                    case StreamingResponseCompletedUpdate done:
                        completed = done.Response;
                        await WriteSseEvent(context, "response_id",
                            new { response_id = done.Response.Id });
                        break;
                }
            }

            if (completed is null) break;

            var approval = completed.OutputItems
                .OfType<McpToolCallApprovalRequestItem>()
                .FirstOrDefault();

            if (approval is null) break;

            await WriteSseEvent(context, "mcp_approval", new
            {
                approval_request_id = approval.Id,
                server_label = approval.ServerLabel,
            });

            // Auto-approve for demo and continue the same response chain.
            options = new CreateResponseOptions { PreviousResponseId = completed.Id };
            options.InputItems.Add(
                ResponseItem.CreateMcpApprovalResponseItem(
                    approvalRequestId: approval.Id,
                    approved: true));
        }

        await WriteSseEvent(context, "done", new { status = "complete" });
    }
    catch (Exception ex)
    {
        await WriteSseEvent(context, "error", new { error = ex.Message });
    }
});

// ── Fallback to index.html for SPA ──────────────────────────────────────────
app.MapFallbackToFile("index.html");

app.Run("http://localhost:5050");

// ── Helpers ──────────────────────────────────────────────────────────────────

static async Task WriteSseEvent(HttpContext context, string eventName, object data)
{
    var json = JsonSerializer.Serialize(data);
    await context.Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n");
    await context.Response.Body.FlushAsync();
}

// ── Request model ────────────────────────────────────────────────────────────

record ChatRequest(string Message, string? ConversationId = null);
