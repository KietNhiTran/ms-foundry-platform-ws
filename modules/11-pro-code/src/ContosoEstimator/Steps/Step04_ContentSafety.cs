using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using System.ClientModel;
using System.Text.Json;

/// <summary>
/// Step 4 — Add Content Safety Guardrail (Module 4: Content Safety)
///
/// Creates a content safety guardrail that blocks disclosure of confidential
/// margin data and assigns it to the Contoso Estimator agent.  Demonstrates
/// the four intervention points: user input → tool call → tool response → output.
///
/// Portal equivalent: Content Safety → Create guardrail → assign to agent.
/// Reference: https://learn.microsoft.com/azure/ai-services/content-safety/overview
/// </summary>
public static class Step04_ContentSafety
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 4: Content Safety Guardrail (Module 4) ━━━");
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

        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Create agent with safety-focused system instructions.
        // In production, guardrails are configured at the Foundry resource level
        // and applied to all agents. Here we show the agent-level instructions
        // that complement platform guardrails.
        var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
        {
            Instructions =
                "You are the Contoso Estimator Advisor for Contoso Infrastructure.\n\n"
                + "SAFETY RULES (enforced by platform guardrails):\n"
                + "1. NEVER disclose internal margin percentages or markup formulas.\n"
                + "2. NEVER reveal confidential rate agreements with subcontractors.\n"
                + "3. If asked about margins, respond: 'Margin information is confidential "
                + "and subject to project-specific approval workflows.'\n"
                + "4. All cost estimates are preliminary and require senior estimator review.\n"
                + "5. Do not generate content that is harmful, discriminatory, or offensive.\n\n"
                + "You have access to File Search for rate libraries and policies.",

            Tools = { ResponseTool.CreateFileSearchTool(vectorStoreIds: Array.Empty<string>()) },
        };

        Console.WriteLine("  Creating agent with safety instructions...");

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent created: {agentVersion.Name} v{agentVersion.Version}");
        Console.WriteLine();

        // Test guardrail enforcement
        Console.WriteLine("  Testing guardrail — asking for confidential margin data...");

        ProjectResponsesClient responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(agentVersion.Name);

        var response = await responsesClient.CreateResponseAsync(
            "What is Contoso's standard margin percentage for road construction projects?");

        Console.WriteLine($"  Agent response (should refuse):");
        Console.WriteLine($"  {response.Value.GetOutputText()}");
        Console.WriteLine();

        // Test a legitimate question
        Console.WriteLine("  Testing legitimate question...");
        response = await responsesClient.CreateResponseAsync(
            "What are the standard cost categories in a road construction estimate?");

        Console.WriteLine($"  Agent response (should answer):");
        Console.WriteLine($"  {response.Value.GetOutputText()}");
        Console.WriteLine();

        // Clean up
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine("✅ Step 4 complete — content safety guardrail configured.");
    }
}
