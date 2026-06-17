using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Evals;
using System.ClientModel;
using System.Text.Json;

/// <summary>
/// Step 7 — Run Batch Evaluation (Module 7: Evaluation)
///
/// Defines an evaluation configuration with built-in evaluators (fluency,
/// task adherence, violence detection), creates an evaluation, and runs
/// it against the Contoso Estimator agent with a set of test queries.
///
/// Portal equivalent: Evaluation → Create evaluation → select evaluators → run.
/// Reference: https://learn.microsoft.com/azure/foundry/observability/how-to/evaluate-agent
/// </summary>
public static class Step07_Evaluation
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("━━━ Step 7: Batch Evaluation (Module 7) ━━━");
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
        Console.WriteLine($"  Model    : {modelDeployment}");
        Console.WriteLine();

        AIProjectClient projectClient = new(
            endpoint: new Uri(endpoint),
            tokenProvider: new DefaultAzureCredential());

        // Create a simple agent for evaluation
        var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
        {
            Instructions =
                "You are the Contoso Estimator Advisor. Answer estimation questions "
                + "accurately and concisely.",
        };

        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient
            .CreateAgentVersionAsync(
                agentName: agentName,
                options: new(agentDefinition));

        Console.WriteLine($"  Agent created: {agentVersion.Name} v{agentVersion.Version}");
        Console.WriteLine();

        // Get the evaluation client
        var evaluationClient = projectClient.ProjectOpenAIClient.GetEvaluationClient();

        // Define testing criteria — built-in evaluators
        object[] testingCriteria =
        [
            new
            {
                type = "azure_ai_evaluator",
                name = "violence_detection",
                evaluator_name = "builtin.violence",
                data_mapping = new
                {
                    query = "{{item.query}}",
                    response = "{{sample.output_text}}",
                },
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "fluency",
                evaluator_name = "builtin.fluency",
                initialization_parameters = new { deployment_name = modelDeployment },
                data_mapping = new
                {
                    query = "{{item.query}}",
                    response = "{{sample.output_text}}",
                },
            },
            new
            {
                type = "azure_ai_evaluator",
                name = "task_adherence",
                evaluator_name = "builtin.task_adherence",
                initialization_parameters = new { deployment_name = modelDeployment },
                data_mapping = new
                {
                    query = "{{item.query}}",
                    response = "{{sample.output_items}}",
                },
            },
        ];

        // Define the data source pointing to the agent
        object dataSource = new
        {
            type = "azure_ai_target_completions",
            source = new
            {
                type = "file_content",
                content = new[]
                {
                    new { item = new { query = "What are typical earthworks rates for road construction?" } },
                    new { item = new { query = "Calculate the cost of 200m of concrete kerbing at $45/m." } },
                    new { item = new { query = "What safety standards apply to bridge construction estimates?" } },
                    new { item = new { query = "Compare labor rates for concrete work across regions." } },
                    new { item = new { query = "What contingency percentage should I apply to a highway project?" } },
                },
            },
            input_messages = new
            {
                type = "template",
                template = new[]
                {
                    new
                    {
                        type = "message",
                        role = "user",
                        content = new { type = "input_text", text = "{{item.query}}" },
                    },
                },
            },
            target = new
            {
                type = "azure_ai_agent",
                name = agentVersion.Name,
                version = agentVersion.Version,
            },
        };

        // Create the evaluation
        Console.WriteLine("  Creating evaluation with 3 evaluators:");
        Console.WriteLine("    - Violence detection");
        Console.WriteLine("    - Fluency");
        Console.WriteLine("    - Task adherence");
        Console.WriteLine();

        BinaryData evaluationConfig = BinaryData.FromObjectAsJson(new
        {
            name = "Contoso Estimator Evaluation",
            data_source_config = new
            {
                type = "custom",
                item_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string" },
                    },
                    required = new[] { "query" },
                },
                include_sample_schema = true,
            },
            testing_criteria = testingCriteria,
        });

        using BinaryContent evaluationContent = BinaryContent.Create(evaluationConfig);
        ClientResult evaluationResult = await evaluationClient
            .CreateEvaluationAsync(evaluationContent);

        using JsonDocument evalDoc = JsonDocument.Parse(
            evaluationResult.GetRawResponse().Content);
        string evaluationId = evalDoc.RootElement.GetProperty("id").GetString()!;
        string evaluationName = evalDoc.RootElement.GetProperty("name").GetString()!;

        Console.WriteLine($"  Evaluation created: {evaluationName} (id: {evaluationId})");
        Console.WriteLine();

        // Create and run the evaluation
        Console.WriteLine("  Running evaluation against agent...");
        Console.WriteLine("  (5 test queries × 3 evaluators)");

        BinaryData runData = BinaryData.FromObjectAsJson(new
        {
            eval_id = evaluationId,
            name = $"Contoso Estimator Eval Run - {DateTime.UtcNow:yyyy-MM-dd}",
            data_source = dataSource,
        });

        using BinaryContent runContent = BinaryContent.Create(runData);
        ClientResult runResult = await evaluationClient.CreateEvaluationRunAsync(
            evaluationId, runContent);

        using JsonDocument runDoc = JsonDocument.Parse(
            runResult.GetRawResponse().Content);
        string runId = runDoc.RootElement.GetProperty("id").GetString()!;

        Console.WriteLine($"  Evaluation run started: {runId}");
        Console.WriteLine("  Check results in Azure Portal → Foundry → Evaluations");
        Console.WriteLine();

        // Clean up agent
        projectClient.AgentAdministrationClient.DeleteAgentVersion(
            agentName: agentVersion.Name,
            agentVersion: agentVersion.Version);

        Console.WriteLine("✅ Step 7 complete — batch evaluation run submitted.");
    }
}
