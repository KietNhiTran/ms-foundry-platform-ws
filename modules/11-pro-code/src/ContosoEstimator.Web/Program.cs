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
var projectClient = new AIProjectClient(new Uri(projectEndpoint), credential);
var openAIClient = projectClient.GetOpenAIClient();

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
    var conversationsClient = openAIClient.GetConversationsClient();
    var conversation = await conversationsClient.CreateConversationAsync();
    return Results.Ok(new { conversation_id = conversation.Value.Id });
});

// ── Delete conversation ──────────────────────────────────────────────────────
app.MapDelete("/api/conversations/{conversationId}", async (string conversationId) =>
{
    var conversationsClient = openAIClient.GetConversationsClient();
    await conversationsClient.DeleteConversationAsync(conversationId);
    return Results.Ok(new { deleted = true });
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
        var conversationsClient = openAIClient.GetConversationsClient();
        var conversation = await conversationsClient.CreateConversationAsync();
        conversationId = conversation.Value.Id;
    }

    // Send conversation_id event
    await WriteSseEvent(context, "conversation_id",
        new { conversation_id = conversationId });

    try
    {
        var responsesClient = openAIClient.GetResponsesClient();

        // Create response with streaming
        await foreach (var streamEvent in responsesClient.CreateResponseStreamingAsync(
            input: req.Message,
            options: new()
            {
                ConversationId = conversationId,
                AgentName = agentName,
            }))
        {
            // Handle text deltas
            if (streamEvent.Delta is not null)
            {
                await WriteSseEvent(context, "delta",
                    new { text = streamEvent.Delta });
            }

            // Handle MCP approval requests (auto-approve for demo)
            if (streamEvent.Item?.Type == "mcp_approval_request")
            {
                await WriteSseEvent(context, "mcp_approval", new
                {
                    approval_request_id = streamEvent.Item.Id,
                    server_label = streamEvent.Item.ServerLabel ?? "Tool",
                    name = streamEvent.Item.Name ?? "",
                });
            }

            // Capture response ID
            if (streamEvent.Response?.Id is not null)
            {
                await WriteSseEvent(context, "response_id",
                    new { response_id = streamEvent.Response.Id });
            }
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
