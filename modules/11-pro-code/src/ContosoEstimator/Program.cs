// ============================================================
// Contoso Estimator — Pro-Code Demo
// 
// This application demonstrates the SDK equivalent of every
// portal-led module in the workshop.
//
// HOW TO USE:
// Uncomment ONE step at a time to demonstrate each capability.
// Each step maps to a portal-led module from the workshop.
// ============================================================

using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine("=== Contoso Estimator — Pro-Code Demo ===");
Console.WriteLine($"Project Endpoint: {config["Foundry:ProjectEndpoint"]}");
Console.WriteLine();

// ============================================================
// Step 1: First API Call (Module 1 - Foundry Setup)
// Portal equivalent: Playground → send a message
// ============================================================
// await ContosoEstimator.Steps.Step01_FirstApiCall.RunAsync(config);

// ============================================================
// Step 2: Create Agent with File Search (Module 2 - Build Agent)
// Portal equivalent: Build → Agents → Create Agent + upload files
// ============================================================
// await ContosoEstimator.Steps.Step02_CreateAgent.RunAsync(config);

// ============================================================
// Step 3: Connect Foundry IQ Knowledge Base (Module 3 - RAG)
// Portal equivalent: Agent → Knowledge → Connect to Foundry IQ
// ============================================================
// await ContosoEstimator.Steps.Step03_FoundryIQ.RunAsync(config);

// ============================================================
// Step 4: Add Content Safety Guardrail (Module 4 - Safety)
// Portal equivalent: Agent → Guardrails → Create guardrail
// ============================================================
// await ContosoEstimator.Steps.Step04_ContentSafety.RunAsync(config);

// ============================================================
// Step 5: Wire Additional Tools (Module 5 - Toolkit)
// Portal equivalent: Agent → Add tool → Code Interpreter / OpenAPI
// ============================================================
// await ContosoEstimator.Steps.Step05_AddTools.RunAsync(config);

// ============================================================
// Step 6: Add Tracing Instrumentation (Module 6 - Observability)
// Portal equivalent: Project Settings → Tracing → Connect App Insights
// ============================================================
// await ContosoEstimator.Steps.Step06_Tracing.RunAsync(config);

// ============================================================
// Step 7: Run Batch Evaluation (Module 7 - Evaluation)
// Portal equivalent: Build → Evaluations → Create evaluation
// ============================================================
// await ContosoEstimator.Steps.Step07_Evaluation.RunAsync(config);

// ============================================================
// Step 8: Full Streaming Chat UI (Bonus - Production Pattern)
// No portal equivalent — this is production-grade client code
// ============================================================
await ContosoEstimator.Steps.Step08_StreamingChat.RunAsync(config);
