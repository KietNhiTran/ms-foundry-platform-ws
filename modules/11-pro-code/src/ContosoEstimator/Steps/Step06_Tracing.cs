/// <summary>
/// Module 6: Observability & Tracing (Pro-Code Version)
/// Portal equivalent: Project Settings → Tracing → Connect App Insights
/// 
/// This step shows how to add OpenTelemetry instrumentation to your
/// client application for custom spans and correlation.
/// </summary>

using Microsoft.Extensions.Configuration;

namespace ContosoEstimator.Steps;

public static class Step06_Tracing
{
    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 6: Tracing Instrumentation (Module 6) ===\n");

        // In a real application, you'd configure OpenTelemetry in Program.cs:
        //
        // builder.Services.AddOpenTelemetry()
        //     .UseAzureMonitor(options =>
        //     {
        //         options.ConnectionString = config["AppInsights:ConnectionString"];
        //     })
        //     .WithTracing(tracing =>
        //     {
        //         tracing.AddSource("ContosoEstimator");
        //         tracing.AddHttpClientInstrumentation();
        //     });

        Console.WriteLine("OpenTelemetry configuration pattern:");
        Console.WriteLine();
        Console.WriteLine("  // In your ASP.NET Core app:");
        Console.WriteLine("  builder.Services.AddOpenTelemetry()");
        Console.WriteLine("      .UseAzureMonitor(o => o.ConnectionString = \"...\")");
        Console.WriteLine("      .WithTracing(t => {");
        Console.WriteLine("          t.AddSource(\"ContosoEstimator\");");
        Console.WriteLine("          t.AddHttpClientInstrumentation();");
        Console.WriteLine("      });");
        Console.WriteLine();
        Console.WriteLine("Custom spans for business context:");
        Console.WriteLine();
        Console.WriteLine("  using var activity = ActivitySource.StartActivity(\"EstimationQuery\");");
        Console.WriteLine("  activity?.SetTag(\"query.category\", \"rate_lookup\");");
        Console.WriteLine("  activity?.SetTag(\"query.region\", \"NSW\");");
        Console.WriteLine("  activity?.SetTag(\"user.team\", \"estimating\");");
        Console.WriteLine();
        Console.WriteLine("Server-side tracing (automatic — zero code):");
        Console.WriteLine("  • Tool calls with latency");
        Console.WriteLine("  • Token usage (prompt + completion)");
        Console.WriteLine("  • LLM reasoning spans");
        Console.WriteLine();
        Console.WriteLine("Client-side tracing (this code):");
        Console.WriteLine("  • Correlation IDs across requests");
        Console.WriteLine("  • Business context (team, query category)");
        Console.WriteLine("  • End-to-end latency from user perspective");
        Console.WriteLine("\n✅ Step 6 complete — tracing pattern demonstrated.\n");

        await Task.CompletedTask;
    }
}
