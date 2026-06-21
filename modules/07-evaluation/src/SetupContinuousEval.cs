// SetupContinuousEval.cs
// Sets up continuous evaluation for the Contoso Estimator agent using the Foundry .NET SDK.
// Prerequisites:
//   - dotnet add package Azure.AI.Projects
//   - dotnet add package Azure.AI.Projects.Agents
//   - dotnet add package Azure.AI.Extensions.OpenAI
//   - dotnet add package Azure.Identity
//
// Environment variables:
//   AZURE_AI_PROJECT_ENDPOINT - Foundry project endpoint URL
//   AZURE_AI_AGENT_NAME - Agent name (e.g., "contoso-estimator-advisor")
//   AZURE_AI_MODEL_DEPLOYMENT_NAME - Judge model deployment name (e.g., "gpt-5.4")

using System.ClientModel;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using OpenAI.Evals;

var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");

var agentName = Environment.GetEnvironmentVariable("AZURE_AI_AGENT_NAME")
    ?? throw new InvalidOperationException("AZURE_AI_AGENT_NAME is not set.");

// Create clients
AIProjectClient projectClient = new(new Uri(endpoint), new DefaultAzureCredential());

#pragma warning disable OPENAI001 // Suppress experimental API warning (preview)
EvaluationClient evaluationClient = projectClient.ProjectOpenAIClient.GetEvaluationClient();
#pragma warning restore OPENAI001

// Step 1: Create the evaluation object with evaluators
Console.WriteLine("Creating evaluation object with evaluators...");

BinaryData evaluationConfig = BinaryData.FromObjectAsJson(new
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

using BinaryContent evaluationContent = BinaryContent.Create(evaluationConfig);
ClientResult evaluationResult = await evaluationClient.CreateEvaluationAsync(evaluationContent);

using JsonDocument evalDoc = JsonDocument.Parse(
    evaluationResult.GetRawResponse().Content);
string evaluationId = evalDoc.RootElement.GetProperty("id").GetString()!;
string evaluationName = evalDoc.RootElement.GetProperty("name").GetString()!;
Console.WriteLine($"  Evaluation created (id: {evaluationId}, name: {evaluationName})");

// Step 2: Create the continuous evaluation rule
Console.WriteLine("Creating continuous evaluation rule...");

ContinuousEvaluationRuleAction continuousAction = new(evaluationId)
{
    MaxHourlyRuns = 50,
};

EvaluationRule continuousRule = new(
    action: continuousAction,
    eventType: EvaluationRuleEventType.ResponseCompleted,
    enabled: true)
{
    Filter = new EvaluationRuleFilter(agentName: agentName),
    DisplayName = "Contoso Estimator Continuous Eval",
    Description = "Auto-evaluates agent responses for quality and groundedness",
};

EvaluationRule createdRule = await projectClient.EvaluationRules.CreateOrUpdateAsync(
    id: "contoso-estimator-continuous-eval",
    evaluationRule: continuousRule);

Console.WriteLine($"  Rule created (id: {createdRule.Id}, name: {createdRule.DisplayName})");
Console.WriteLine();
Console.WriteLine("Continuous evaluation is now active.");
Console.WriteLine("Generate agent traffic, then check the Monitor tab in ~5 minutes.");
