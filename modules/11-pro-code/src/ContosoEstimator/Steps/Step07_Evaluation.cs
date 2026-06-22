/// <summary>
/// Module 7: Batch Evaluation (Pro-Code Version)
/// Portal equivalent: Build → Evaluations → Create evaluation
///
/// Submits a batch evaluation against the deployed agent using the labeled
/// JSONL dataset shipped with Module 7. The run is fire-and-forget — check
/// progress and final scores in the Foundry portal (Evaluations tab).
///
/// Pattern (Azure.AI.Projects 2.x preview):
///   1. CreateEvaluationAsync          — define evaluators + dataset schema
///   2. CreateEvaluationRunAsync       — point a run at the agent with the dataset
/// </summary>

using System.ClientModel;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Evals;

namespace ContosoEstimator.Steps;

public static class Step07_Evaluation
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 7: Batch Evaluation (Module 7) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";
        var datasetPath = ResolveDatasetPath(
            config["Evaluation:DatasetPath"] ?? "modules/07-evaluation/data/contoso-estimator-eval.jsonl");

        Console.WriteLine($"Agent:    {agentName}");
        Console.WriteLine($"Judge:    {modelDeployment}");
        Console.WriteLine($"Dataset:  {datasetPath}\n");

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

#pragma warning disable OPENAI001 // Preview API
        var evaluationClient = projectClient.ProjectOpenAIClient.GetEvaluationClient();
#pragma warning restore OPENAI001

        // ── [1/3] Load JSONL dataset ─────────────────────────────────────
        Console.WriteLine("[1/3] Loading evaluation dataset...");
        var items = LoadJsonlDataset(datasetPath);
        Console.WriteLine($"  Loaded {items.Count} test cases.");

        // ── [2/3] Create the evaluation definition ───────────────────────
        // data_source_config.type = "custom" lets us bring our own dataset
        // schema (query + expected_answer + category + expected_tool).
        // include_sample_schema = true gives evaluators access to the agent's
        // runtime output via {{sample.output_text}} / {{sample.output_items}}.
        Console.WriteLine("[2/3] Creating evaluation definition...");
        var evaluationConfig = BinaryData.FromObjectAsJson(new
        {
            name = $"Contoso Estimator Batch Eval ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
            data_source_config = new
            {
                type = "custom",
                item_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        query           = new { type = "string" },
                        expected_answer = new { type = "string" },
                        category        = new { type = "string" },
                        expected_tool   = new { type = "string" },
                    },
                    required = new[] { "query" }
                },
                include_sample_schema = true
            },
            testing_criteria = new object[]
            {
                new
                {
                    type = "azure_ai_evaluator",
                    name = "task_adherence",
                    evaluator_name = "builtin.task_adherence",
                    initialization_parameters = new { deployment_name = modelDeployment },
                    data_mapping = new
                    {
                        query    = "{{item.query}}",
                        response = "{{sample.output_items}}",
                    }
                },
                new
                {
                    type = "azure_ai_evaluator",
                    name = "tool_call_accuracy",
                    evaluator_name = "builtin.tool_call_accuracy",
                    initialization_parameters = new { deployment_name = modelDeployment },
                    data_mapping = new
                    {
                        query            = "{{item.query}}",
                        response         = "{{sample.output_items}}",
                        tool_definitions = "{{sample.tool_definitions}}",
                    }
                },
                new
                {
                    type = "azure_ai_evaluator",
                    name = "groundedness",
                    evaluator_name = "builtin.groundedness",
                    initialization_parameters = new { deployment_name = modelDeployment },
                    data_mapping = new
                    {
                        query        = "{{item.query}}",
                        response     = "{{sample.output_text}}",
                        ground_truth = "{{item.expected_answer}}",
                    }
                },
                new
                {
                    type = "azure_ai_evaluator",
                    name = "coherence",
                    evaluator_name = "builtin.coherence",
                    initialization_parameters = new { deployment_name = modelDeployment },
                    data_mapping = new
                    {
                        query    = "{{item.query}}",
                        response = "{{sample.output_text}}",
                    }
                },
                new
                {
                    type = "azure_ai_evaluator",
                    name = "intent_resolution",
                    evaluator_name = "builtin.intent_resolution",
                    initialization_parameters = new { deployment_name = modelDeployment },
                    data_mapping = new
                    {
                        query    = "{{item.query}}",
                        response = "{{sample.output_text}}",
                    }
                },
            }
        });

        using var evaluationContent = BinaryContent.Create(evaluationConfig);
        var evaluationResult = await evaluationClient.CreateEvaluationAsync(evaluationContent);
        using var evalDoc = JsonDocument.Parse(evaluationResult.GetRawResponse().Content);
        var evaluationId = evalDoc.RootElement.GetProperty("id").GetString()!;
        Console.WriteLine($"  Evaluation created (id: {evaluationId})");

        // ── [3/3] Kick off a run against the agent ───────────────────────
        // data_source.type = "azure_ai_target_completions" tells Foundry to
        //   (a) call the agent once per dataset row with the templated user
        //       message, then (b) score each response with the evaluators
        //       above. This is the same shape continuous eval uses, but with
        //       a static file_content source instead of live traces.
        Console.WriteLine("[3/3] Submitting evaluation run against agent...");
        var runName = $"batch-eval-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var runData = BinaryData.FromObjectAsJson(new
        {
            eval_id = evaluationId,
            name = runName,
            data_source = new
            {
                type = "azure_ai_target_completions",
                source = new
                {
                    type = "file_content",
                    content = items.Select(it => new { item = it }).ToArray(),
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
                            content = new { type = "input_text", text = "{{item.query}}" }
                        }
                    }
                },
                target = new
                {
                    type = "azure_ai_agent",
                    name = agentName,
                }
            }
        });

        using var runDataContent = BinaryContent.Create(runData);
        var runResult = await evaluationClient.CreateEvaluationRunAsync(
            evaluationId: evaluationId, content: runDataContent);
        using var runDoc = JsonDocument.Parse(runResult.GetRawResponse().Content);
        var runId = runDoc.RootElement.GetProperty("id").GetString()!;
        var runStatus = runDoc.RootElement.GetProperty("status").GetString() ?? "queued";

        Console.WriteLine($"  Run submitted (id: {runId}, status: {runStatus})");
        if (runDoc.RootElement.TryGetProperty("report_url", out var reportUrl))
            Console.WriteLine($"  Report URL: {reportUrl.GetString()}");

        Console.WriteLine($"\n✅ Step 7 complete — evaluation '{runName}' submitted.");
        Console.WriteLine("    Track progress in Foundry portal → Evaluations tab.\n");
    }

    // Parse each JSONL line as an arbitrary JSON object and clone the element
    // so the per-line JsonDocument can be disposed.
    private static List<JsonElement> LoadJsonlDataset(string path)
    {
        var items = new List<JsonElement>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            items.Add(doc.RootElement.Clone());
        }
        if (items.Count == 0)
            throw new InvalidOperationException($"Dataset '{path}' is empty.");
        return items;
    }

    // Same resolution strategy as ResolveKnowledgeBasePath in Step02/Step03:
    // check absolute, then walk up from bin/cwd to find modules/07-evaluation/data.
    private static string ResolveDatasetPath(string configured)
    {
        if (Path.IsPathRooted(configured) && File.Exists(configured))
            return Path.GetFullPath(configured);

        foreach (var anchor in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var candidate = Path.GetFullPath(Path.Combine(anchor, configured));
            if (File.Exists(candidate)) return candidate;

            var fileName = Path.GetFileName(configured);
            var dir = new DirectoryInfo(anchor);
            while (dir != null)
            {
                var probe = Path.Combine(dir.FullName, "modules", "07-evaluation", "data", fileName);
                if (File.Exists(probe)) return probe;
                dir = dir.Parent;
            }
        }

        throw new FileNotFoundException(
            $"Could not locate evaluation dataset '{configured}'. " +
            $"Set 'Evaluation:DatasetPath' to an absolute path.", configured);
    }
}
