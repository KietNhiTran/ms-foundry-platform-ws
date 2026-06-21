/// <summary>
/// Module 7b: Continuous Evaluation Setup (Pro-Code Version)
/// Portal equivalent: Agent → Monitor → Settings → Continuous evaluation
/// 
/// This step creates a continuous evaluation rule that auto-scores
/// production agent traffic. It evaluates existing traces — the agent
/// is NOT re-invoked, so there's no additional cost from tool calls.
/// </summary>

using System.ClientModel;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Evals;

namespace ContosoEstimator.Steps;

public static class Step07b_ContinuousEvaluation
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 7b: Continuous Evaluation (Module 7) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

#pragma warning disable OPENAI001 // Suppress experimental API warning (preview)
        var evaluationClient = projectClient.ProjectOpenAIClient.GetEvaluationClient();
#pragma warning restore OPENAI001

        // ── Step 1: Create the evaluation object ─────────────────────────
        Console.WriteLine("[1/3] Creating evaluation object with evaluators...");

        var evaluationConfig = BinaryData.FromObjectAsJson(new
        {
            name = "Contoso Estimator Continuous Evaluation",
            data_source_config = new { type = "azure_ai_source", scenario = "responses" },
            testing_criteria = new object[]
            {
                new { type = "azure_ai_evaluator", name = "task_adherence", evaluator_name = "builtin.task_adherence" },
                new { type = "azure_ai_evaluator", name = "tool_call_accuracy", evaluator_name = "builtin.tool_call_accuracy" },
                new { type = "azure_ai_evaluator", name = "groundedness", evaluator_name = "builtin.groundedness" },
                new { type = "azure_ai_evaluator", name = "coherence", evaluator_name = "builtin.coherence" },
                new { type = "azure_ai_evaluator", name = "intent_resolution", evaluator_name = "builtin.intent_resolution" },
            }
        });

        using var evaluationContent = BinaryContent.Create(evaluationConfig);
        var evaluationResult = await evaluationClient.CreateEvaluationAsync(evaluationContent);

        using var evalDoc = JsonDocument.Parse(
            evaluationResult.GetRawResponse().Content);
        var evaluationId = evalDoc.RootElement.GetProperty("id").GetString()!;
        var evaluationName = evalDoc.RootElement.GetProperty("name").GetString()!;
        Console.WriteLine($"  Evaluation created (id: {evaluationId}, name: {evaluationName})");

        // ── Step 2: Create the continuous evaluation rule ────────────────
        Console.WriteLine("[2/3] Creating continuous evaluation rule...");

        var continuousAction = new ContinuousEvaluationRuleAction(evaluationId)
        {
            MaxHourlyRuns = 50,
        };

        var continuousRule = new EvaluationRule(
            action: continuousAction,
            eventType: EvaluationRuleEventType.ResponseCompleted,
            enabled: true)
        {
            Filter = new EvaluationRuleFilter(agentName: agentName),
            DisplayName = "Contoso Estimator Continuous Eval",
            Description = "Auto-evaluates agent responses for quality and groundedness",
        };

        var createdRule = await projectClient.EvaluationRules.CreateOrUpdateAsync(
            id: "contoso-estimator-continuous-eval",
            evaluationRule: continuousRule);

        Console.WriteLine($"  Rule created (id: {createdRule.Id}, name: {createdRule.DisplayName})");

        // ── Step 3: Verify recent runs ───────────────────────────────────
        Console.WriteLine("[3/3] Checking for existing evaluation runs...");

        var runsResult = await evaluationClient.GetEvaluationRunsAsync(
            evaluationId, null, null, null, null, new());

        using var runsDoc = JsonDocument.Parse(
            runsResult.GetRawResponse().Content);
        var runs = runsDoc.RootElement.GetProperty("data");

        if (runs.GetArrayLength() > 0)
        {
            var firstRun = runs[0];
            if (firstRun.TryGetProperty("report_url", out var reportUrlElement))
            {
                Console.WriteLine($"  Latest report: {reportUrlElement.GetString()}");
            }
        }
        else
        {
            Console.WriteLine("  No runs yet — generate agent traffic, then check Monitor tab in ~5 min.");
        }

        Console.WriteLine();
        Console.WriteLine("Continuous evaluation is now active.");
        Console.WriteLine("  • Evaluators: task_adherence, tool_call_accuracy, groundedness, coherence, intent_resolution");
        Console.WriteLine("  • Trigger: every agent response completion");
        Console.WriteLine("  • Max hourly runs: 50");
        Console.WriteLine("  • Results: Foundry portal → Agent → Monitor tab");
        Console.WriteLine();
        Console.WriteLine("To clean up:");
        Console.WriteLine("  await projectClient.EvaluationRules.DeleteAsync(id: \"contoso-estimator-continuous-eval\");");
        Console.WriteLine("\n✅ Step 7b complete — continuous evaluation rule active.\n");
    }
}
