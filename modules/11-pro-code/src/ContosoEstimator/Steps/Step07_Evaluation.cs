/// <summary>
/// Module 7: Evaluation & Continuous Monitoring (Pro-Code Version)
/// Portal equivalent: Build → Evaluations → Create evaluation
/// 
/// This step demonstrates running a batch evaluation against the agent
/// using a labeled dataset — the pattern for CI/CD integration.
/// </summary>

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ContosoEstimator.Steps;

public static class Step07_Evaluation
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 7: Batch Evaluation (Module 7) ===\n");

        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        // Load evaluation dataset
        var evalDatasetPath = config["Evaluation:DatasetPath"] 
            ?? "../../modules/07-evaluation/data/contoso-estimator-eval.jsonl";

        Console.WriteLine($"Evaluation dataset: {evalDatasetPath}");
        Console.WriteLine();

        // In production, the evaluation SDK call looks like:
        //
        // var evaluation = await projectClient.Evaluations.CreateAsync(
        //     new EvaluationDefinition
        //     {
        //         Name = "estimator-eval-ci",
        //         AgentName = "contoso-estimator-advisor",
        //         DatasetPath = evalDatasetPath,
        //         Evaluators = 
        //         {
        //             "groundedness",
        //             "relevance", 
        //             "tool_call_accuracy"
        //         }
        //     }
        // );
        //
        // // Wait for completion
        // var result = await projectClient.Evaluations.WaitForCompletionAsync(evaluation.Value.Id);
        //
        // // Check pass/fail for CI/CD gate
        // var avgGroundedness = result.Value.Metrics["groundedness"].Average;
        // if (avgGroundedness < 3.5)
        // {
        //     Console.WriteLine("❌ FAILED: Groundedness below threshold");
        //     Environment.Exit(1);
        // }

        Console.WriteLine("Evaluation pattern for CI/CD:");
        Console.WriteLine();
        Console.WriteLine("  1. Load JSONL dataset (queries + expected answers)");
        Console.WriteLine("  2. Create evaluation run against agent");
        Console.WriteLine("  3. Select evaluators: groundedness, relevance, tool_call_accuracy");
        Console.WriteLine("  4. Wait for completion");
        Console.WriteLine("  5. Check scores against threshold (e.g., groundedness ≥ 3.5)");
        Console.WriteLine("  6. Pass/fail the deployment gate");
        Console.WriteLine();
        Console.WriteLine("Dataset categories:");
        Console.WriteLine("  • rate_lookup (10 queries) — validate File Search retrieval");
        Console.WriteLine("  • policy (8 queries) — validate policy document answers");
        Console.WriteLine("  • calculation (6 queries) — validate Code Interpreter math");
        Console.WriteLine("  • boundary (4 queries) — validate agent refuses correctly");
        Console.WriteLine("  • cross_document (4 queries) — validate multi-doc synthesis");
        Console.WriteLine("\n✅ Step 7 complete — evaluation pattern for CI/CD demonstrated.\n");

        await Task.CompletedTask;
    }
}
