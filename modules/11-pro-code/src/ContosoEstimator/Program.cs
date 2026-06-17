// ============================================================
// Contoso Estimator — Pro-Code Demo
// Uncomment ONE step at a time to demonstrate each capability.
// Each step maps to a portal-led module from the workshop.
// ============================================================

using Microsoft.Extensions.Configuration;

// Load configuration from appsettings.json and environment variables
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║   Contoso Estimator — Pro-Code Demo                 ║");
Console.WriteLine("║   Uncomment one step at a time in Program.cs        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// ── Step 1: First API Call (Module 1 — Foundry Setup) ───────────────
// Makes a simple chat completion call using the OpenAI client
// pointed at your Foundry model deployment.
// await Step01_FirstApiCall.RunAsync(config);

// ── Step 2: Create Agent with File Search (Module 2 — Build Agent) ──
// Creates the Contoso Estimator agent with File Search tool
// and uploads rate library / policy documents.
// await Step02_CreateAgent.RunAsync(config);

// ── Step 3: Connect Foundry IQ Knowledge Base (Module 3 — RAG) ──────
// Adds an MCP tool that connects to a Foundry IQ knowledge base
// for enterprise-grade retrieval over project history.
// await Step03_FoundryIQ.RunAsync(config);

// ── Step 4: Add Content Safety Guardrail (Module 4 — Safety) ────────
// Creates a content safety guardrail to block disclosure of
// confidential margin data and assigns it to the agent.
// await Step04_ContentSafety.RunAsync(config);

// ── Step 5: Wire Additional Tools (Module 5 — Toolkit) ─────────────
// Adds Code Interpreter for cost calculations and an OpenAPI tool
// for external pricing data.
// await Step05_AddTools.RunAsync(config);

// ── Step 6: Add Tracing Instrumentation (Module 6 — Observability) ──
// Configures OpenTelemetry with Azure Monitor exporter to trace
// all agent interactions into Application Insights.
// await Step06_Tracing.RunAsync(config);

// ── Step 7: Run Batch Evaluation (Module 7 — Evaluation) ────────────
// Defines an evaluation with built-in evaluators (fluency,
// task adherence) and runs it against the agent.
// await Step07_Evaluation.RunAsync(config);

// ── Step 8: Full Streaming Chat UI (Bonus — Production Pattern) ─────
// Runs an interactive streaming chat loop using the Responses API.
await Step08_StreamingChat.RunAsync(config);
