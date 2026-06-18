/// <summary>
/// Module 4: Content Safety & Guardrails (Pro-Code Version)
/// Portal equivalent: Agent → Guardrails → Create guardrail
/// 
/// This step demonstrates how guardrails would be configured programmatically.
/// Note: Guardrail creation via SDK may require preview APIs.
/// </summary>

using Microsoft.Extensions.Configuration;

namespace ContosoEstimator.Steps;

public static class Step04_ContentSafety
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 4: Content Safety Guardrail (Module 4) ===\n");

        // Note: As of June 2026, guardrail creation is primarily done via portal.
        // The SDK supports querying content safety but guardrail assignment 
        // to agents is portal-only in most scenarios.
        //
        // This step demonstrates the pattern for when SDK support is available.

        Console.WriteLine("Guardrail configuration for Contoso Estimator:");
        Console.WriteLine("  Rule 1: Block requests for margin/markup percentages");
        Console.WriteLine("  Rule 2: Block competitor pricing requests");
        Console.WriteLine("  Rule 3: Block bulk data export requests");
        Console.WriteLine("  Rule 4: Detect document-embedded prompt injection");
        Console.WriteLine();
        Console.WriteLine("Intervention points:");
        Console.WriteLine("  • User Input → Block jailbreak attempts");
        Console.WriteLine("  • Tool Response → Redact sensitive fields");
        Console.WriteLine("  • Output → Block margin disclosure in responses");
        Console.WriteLine();

        // In production, you'd use the Content Safety SDK:
        // var safetyClient = new ContentSafetyClient(endpoint, credential);
        // await safetyClient.AnalyzeTextAsync(new AnalyzeTextOptions { ... });

        Console.WriteLine("⚠️  Guardrail creation is currently portal-only.");
        Console.WriteLine("    Use the portal (Module 4) to configure, then manage via SDK.");
        Console.WriteLine("\n✅ Step 4 complete — guardrail pattern demonstrated.\n");

        await Task.CompletedTask;
    }
}
