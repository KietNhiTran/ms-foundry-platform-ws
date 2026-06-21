// CleanupContinuousEval.cs
// Removes the continuous evaluation rule created by SetupContinuousEval.cs.
// Safe to run if the rule doesn't exist.

using Azure.AI.Projects;
using Azure.Identity;

var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");

AIProjectClient projectClient = new(new Uri(endpoint), new DefaultAzureCredential());

try
{
    await projectClient.EvaluationRules.DeleteAsync(id: "contoso-estimator-continuous-eval");
    Console.WriteLine("Continuous evaluation rule 'contoso-estimator-continuous-eval' deleted.");
}
catch (Azure.RequestFailedException ex) when (ex.Status == 404)
{
    Console.WriteLine("Rule not found — nothing to clean up.");
}
