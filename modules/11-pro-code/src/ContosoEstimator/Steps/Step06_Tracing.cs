/// <summary>
/// Module 6: Observability & Tracing (Pro-Code Version) — runnable.
///
/// Portal equivalent: Project Settings → Tracing → Connect App Insights.
/// Server-side spans (LLM reasoning, tool calls, token usage) are emitted by
/// Foundry automatically once the project is linked to App Insights. This step
/// adds the CLIENT-SIDE half: business context spans + HTTP instrumentation +
/// Azure Monitor exporter, so end-to-end traces correlate from your user-facing
/// query down to the model call.
///
/// Run this after Step 2 (the agent must exist) — it sends one query through the
/// agent inside a custom "EstimationQuery" span and flushes traces to App Insights.
/// </summary>

using System.Diagnostics;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step06_Tracing
{
    // Single shared ActivitySource for all custom client-side spans. The name must
    // match the AddSource() call below — that's how OpenTelemetry decides which
    // ActivitySource instances to subscribe to.
    private const string ActivitySourceName = "ContosoEstimator";
    private static readonly ActivitySource ContosoActivitySource = new(ActivitySourceName, "1.0.0");

    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 6: Tracing Instrumentation (Module 6) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";
        var appInsightsConnectionString = config["AppInsights:ConnectionString"];

        if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            Console.WriteLine("⚠️  AppInsights:ConnectionString not set — traces will only be written to the console.");
        }

        // 1. Build the TracerProvider. In an ASP.NET Core app you'd use
        //    builder.Services.AddOpenTelemetry().UseAzureMonitor(...); the console
        //    equivalent is OpenTelemetry.Sdk.CreateTracerProviderBuilder() plus the
        //    standalone AzureMonitorTraceExporter.
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "contoso-estimator-trace-test", serviceVersion: "1.0.0"))
            .AddSource(ActivitySourceName)
            .AddHttpClientInstrumentation();  // captures outbound calls to Foundry

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            tracerBuilder.AddAzureMonitorTraceExporter(o => o.ConnectionString = appInsightsConnectionString);
        }

        using var tracerProvider = tracerBuilder.Build();

        // 2. Connect to the agent created in Step 2.
        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        var conversationClient = projectClient.ProjectOpenAIClient.GetProjectConversationsClient();
        ProjectConversation conversation = (await conversationClient.CreateProjectConversationAsync()).Value;

        var responsesClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(
            defaultAgent: agentName,
            defaultConversationId: conversation.Id);

        // 3. Run a sample query inside a custom span. Tags here become searchable
        //    dimensions in App Insights (customDimensions in KQL) — use them for
        //    business context that the auto-instrumented spans can't infer.
        var userQuery = "What is the labour rate for a Carpenter in NSW?";

        using (var activity = ContosoActivitySource.StartActivity("EstimationQuery", ActivityKind.Client))
        {
            activity?.SetTag("query.category", "rate_lookup");
            activity?.SetTag("query.region", "NSW");
            activity?.SetTag("query.trade", "Carpenter");
            activity?.SetTag("user.team", "estimating");
            activity?.SetTag("agent.name", agentName);
            activity?.SetTag("conversation.id", conversation.Id);

            Console.WriteLine($"User: {userQuery}\n");

            try
            {
                // No `model` argument — when an agent is bound to the responses client
                // the model comes from the agent definition. Passing one causes
                // "Model must match the agent's model" (HTTP 400 invalid_payload).
                var responseOptions = new CreateResponseOptions();
                responseOptions.InputItems.Add(ResponseItem.CreateUserMessageItem(userQuery));

                var response = (await responsesClient.CreateResponseAsync(responseOptions)).Value;

                // Auto-approve Foundry IQ MCP calls if the agent asks for approval.
                foreach (ResponseItem item in response.OutputItems)
                {
                    if (item is McpToolCallApprovalRequestItem mcpApproval)
                    {
                        activity?.AddEvent(new ActivityEvent("mcp.approval.auto_approved",
                            tags: new ActivityTagsCollection { { "server", mcpApproval.ServerLabel } }));

                        var approvalOptions = new CreateResponseOptions { PreviousResponseId = response.Id };
                        approvalOptions.InputItems.Add(
                            ResponseItem.CreateMcpApprovalResponseItem(
                                approvalRequestId: mcpApproval.Id,
                                approved: true));

                        response = (await responsesClient.CreateResponseAsync(approvalOptions)).Value;
                        break;
                    }
                }

                var answer = response.GetOutputText();
                activity?.SetTag("response.length", answer?.Length ?? 0);
                activity?.SetStatus(ActivityStatusCode.Ok);

                Console.WriteLine($"Assistant: {answer}\n");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                throw;
            }

            Console.WriteLine($"TraceId: {activity?.TraceId}");
            Console.WriteLine($"SpanId : {activity?.SpanId}");
        }

        // 4. Force-flush before exiting — console apps die fast and dropped spans
        //    are the #1 reason "tracing isn't working" tickets get filed.
        tracerProvider.ForceFlush(timeoutMilliseconds: 5000);

        Console.WriteLine("\n✅ Step 6 complete — span exported to console + App Insights.");
        Console.WriteLine("   In App Insights, query: dependencies | where name == \"EstimationQuery\"");
    }
}
