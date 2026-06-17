using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Step 6 — Add Tracing Instrumentation (Module 6: Observability)
///
/// Configures OpenTelemetry with the Azure Monitor exporter to trace all
/// agent interactions into Application Insights.  Shows how to enable
/// GenAI tracing, add custom attributes, and correlate traces.
///
/// Portal equivalent: Settings → Connected resources → Application Insights.
/// Reference: https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-client-side
/// </summary>
public static class Step06_Tracing
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 6: Tracing + Observability (Module 6) ━━━");
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
        Console.WriteLine();

        // Enable GenAI tracing (required for Azure.AI.Projects trace output)
        AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Get the Application Insights connection string from the project
        // (or fall back to configuration)
        var appInsightsConnectionString =
            config["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? config["ApplicationInsights:ConnectionString"];

        if (string.IsNullOrEmpty(appInsightsConnectionString))
        {
            Console.WriteLine("  Fetching App Insights connection string from project...");
            appInsightsConnectionString = await projectClient.Telemetry
                .GetApplicationInsightsConnectionStringAsync();
        }

        Console.WriteLine($"  App Insights connected.");
        Console.WriteLine();

        // Set the environment variable so the Azure Monitor exporter picks it up
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            appInsightsConnectionString);

        // Configure OpenTelemetry with Azure Monitor exporter
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("Azure.AI.Projects.*")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("ContosoEstimator"))
            .AddAzureMonitorTraceExporter()
            .Build();

        using (tracerProvider)
        {
            Console.WriteLine("  OpenTelemetry configured — traces export to App Insights.");
            Console.WriteLine();

            // Create a simple agent to generate traced interactions
            var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
            {
                Instructions =
                    "You are the Contoso Estimator Advisor. Be concise.",
            };

            ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
                .CreateAgentVersionAsync(
                    agentName: agentName,
                    options: new(agentDefinition));

            Console.WriteLine($"  Agent created: {agentVersion.Name} v{agentVersion.Version}");
            Console.WriteLine("  Sending traced request...");

            ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
                .GetProjectResponsesClientForAgent(agentVersion.Name);

            var response = await responsesClient.CreateResponseAsync(
                "Give me a quick cost breakdown for a 100m concrete footpath.");

            Console.WriteLine($"  Agent response:");
            Console.WriteLine($"  {response.Value.GetOutputText()}");
            Console.WriteLine();
            Console.WriteLine("  Trace exported to Application Insights.");
            Console.WriteLine("  View in Azure Portal → Application Insights → Transaction search");
            Console.WriteLine();

            // Clean up
            projectClient.AgentAdministrationClient.DeleteAgentVersion(
                agentName: agentVersion.Name,
                agentVersion: agentVersion.Version);
        }

        Console.WriteLine("✅ Step 6 complete — OpenTelemetry tracing configured.");
    }
}
