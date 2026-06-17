# Module 11: Pro-Code Development (.NET) (45 min)

**Objective:** Build the Contoso Estimator agent programmatically using the Foundry .NET SDK, demonstrating the code equivalent of every portal step from Modules 1–7.

---

## Topics

- SDK overview: `Azure.AI.Projects` (C#) — unified client for agents, models, and tools
- Authentication: `DefaultAzureCredential`, managed identities
- Step-by-step progression: first API call → agent creation → RAG → safety → tools → tracing → evaluation → streaming chat
- Responses API vs legacy Assistants API
- Best practices for production deployments

---

## Demo: Progressive Pro-Code Build

The presenter opens `Program.cs` and uncomments one step at a time. Each step mirrors a previous portal-led module:

| Step | Portal Module | What the Code Does | Key SDK Type |
|------|---------------|--------------------|----|
| 01 | Module 1 | Call chat completions via OpenAI client | `ChatClient` + `BearerTokenPolicy` |
| 02 | Module 2 | Create agent + File Search tool | `AIProjectClient.AgentAdministrationClient` |
| 03 | Module 3 | Create MCP tool pointing to Foundry IQ KB | `ResponseTool.CreateMcpTool()` |
| 04 | Module 4 | Add safety instructions + guardrail patterns | `DeclarativeAgentDefinition.Instructions` |
| 05 | Module 5 | Add Code Interpreter + Web Search tools | `ResponseTool.CreateCodeInterpreterTool()` |
| 06 | Module 6 | Add OpenTelemetry traces + Azure Monitor | `Sdk.CreateTracerProviderBuilder()` |
| 07 | Module 7 | Create evaluation + run batch eval | `EvaluationClient` |
| 08 | Bonus | Interactive streaming chat loop | `ProjectResponsesClient.CreateResponseStreamingAsync()` |

### Suggested Demo Flow

1. Show `Program.cs` with all steps commented
2. Uncomment **Step 01** → run → show first API call working
3. Uncomment **Step 02** → run → agent created with File Search
4. Walk through **Step 03** code (MCP tool) — explain this is what the portal did behind the scenes
5. Skip to **Step 08** → run the streaming chat — show full production pattern
6. Time permitting: show **Step 07** (evaluation) for the CI/CD story

---

## Prerequisites

| # | Requirement | How to Verify |
|---|-------------|---------------|
| 1 | .NET 9 SDK installed | `dotnet --version` → 9.x |
| 2 | Azure CLI signed in | `az account show` |
| 3 | Foundry resource with project | Check [ai.azure.com](https://ai.azure.com) |
| 4 | GPT-4.1 model deployed | Foundry portal → Models |
| 5 | Application Insights (Step 6) | Azure Portal → App Insights |

---

## Quick Start

```bash
# Navigate to the project
cd modules/11-pro-code/src/ContosoEstimator

# Copy and configure environment
cp .env.example .env
# Edit .env with your Foundry endpoint and model deployment

# Restore packages
dotnet restore

# Build
dotnet build

# Run (with Step 8 active by default)
dotnet run
```

---

## Project Structure

```
modules/11-pro-code/src/ContosoEstimator/
├── ContosoEstimator.sln              # Solution file
├── ContosoEstimator.csproj           # Project with NuGet references
├── Program.cs                        # Uncomment one step at a time
├── appsettings.json                  # Configuration
├── .env.example                      # Environment template
└── Steps/
    ├── Step01_FirstApiCall.cs         # Module 1: Deploy model, call chat completions
    ├── Step02_CreateAgent.cs          # Module 2: Create agent + File Search tool
    ├── Step03_FoundryIQ.cs            # Module 3: MCPTool + Foundry IQ KB connection
    ├── Step04_ContentSafety.cs        # Module 4: Safety instructions + guardrails
    ├── Step05_AddTools.cs             # Module 5: Code Interpreter + Web Search
    ├── Step06_Tracing.cs              # Module 6: OpenTelemetry + App Insights
    ├── Step07_Evaluation.cs           # Module 7: Batch eval with built-in evaluators
    └── Step08_StreamingChat.cs        # Bonus: Interactive streaming chat loop
```

---

## Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `Azure.AI.Projects` | Foundry project client, agent management, evaluations |
| `Azure.AI.Projects.Agents` | Agent definitions, tools, runtime components |
| `Azure.AI.Extensions.OpenAI` | OpenAI client extensions for Foundry |
| `Azure.Identity` | `DefaultAzureCredential` for Entra ID auth |
| `Azure.Monitor.OpenTelemetry.Exporter` | Export traces to Application Insights |
| `OpenTelemetry` | Tracing instrumentation |
| `OpenTelemetry.Extensions.Hosting` | OpenTelemetry host integration |
| `Microsoft.Extensions.Configuration` | Configuration loading |

---

## Step Details

### Step 1: First API Call

Authenticates with Entra ID using `DefaultAzureCredential` and calls chat completions through the OpenAI-compatible endpoint. This is the simplest possible Foundry interaction.

```csharp
ChatClient chatClient = new(
    model: modelDeployment,
    authenticationPolicy: tokenPolicy,
    options: new OpenAIClientOptions { Endpoint = openAiEndpoint });

ChatCompletion completion = await chatClient.CompleteChatAsync([
    new SystemChatMessage("You are the Contoso Estimator Advisor..."),
    new UserChatMessage("What are typical cost components in a road estimate?"),
]);
```

### Step 2: Create Agent with File Search

Creates the Contoso Estimator agent programmatically with system instructions and the File Search tool for document retrieval.

```csharp
var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
{
    Instructions = "You are the Contoso Estimator Advisor...",
    Tools = { ResponseTool.CreateFileSearchTool() },
};

AgentVersion agent = await projectClient.AgentAdministrationClient
    .CreateAgentVersionAsync(agentName: agentName, options: new(agentDefinition));
```

### Step 3: Connect Foundry IQ

Adds an MCP tool pointing to a Foundry IQ knowledge base — this is exactly what the portal does when you click "Add Foundry IQ" on the tools page.

```csharp
var mcpTool = ResponseTool.CreateMcpTool(
    serverLabel: "contoso-project-history",
    serverUri: new Uri(foundryIqEndpoint),
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(
        GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
```

### Step 4: Content Safety

Shows the programmatic equivalent of configuring guardrails — system instructions that prevent disclosure of confidential margin data, complementing platform-level content filters.

### Step 5: Additional Tools

Adds Code Interpreter for cost calculations and Web Search for real-time market data, demonstrating multi-tool orchestration.

```csharp
Tools =
{
    ResponseTool.CreateFileSearchTool(),
    ResponseTool.CreateCodeInterpreterTool(),
    ResponseTool.CreateWebSearchTool(),
};
```

### Step 6: Tracing

Configures OpenTelemetry to export traces to Application Insights, enabling full observability of agent interactions.

```csharp
AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Azure.AI.Projects.*")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ContosoEstimator"))
    .AddAzureMonitorTraceExporter()
    .Build();
```

### Step 7: Batch Evaluation

Creates an evaluation with built-in evaluators (violence detection, fluency, task adherence) and runs it against the agent with domain-specific test queries.

### Step 8: Streaming Chat

Runs an interactive chat loop with streaming responses, multi-turn conversation memory, and all tools active — the production pattern for real-time UIs.

```csharp
await foreach (StreamingResponseUpdate update
    in responsesClient.CreateResponseStreamingAsync(userInput))
{
    if (update is StreamingResponseOutputTextDeltaUpdate textDelta)
        Console.Write(textDelta.Delta);
}
```

---

## Reference

| Resource | Link |
|----------|------|
| SDK Overview | [learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview](https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview) |
| .NET SDK Reference | [learn.microsoft.com/dotnet/api/overview/azure/ai.projects-readme](https://learn.microsoft.com/dotnet/api/overview/azure/ai.projects-readme) |
| Quickstart Code | [learn.microsoft.com/azure/foundry/quickstarts/get-started-code](https://learn.microsoft.com/azure/foundry/quickstarts/get-started-code) |
| Responses API | [learn.microsoft.com/azure/foundry/foundry-models/how-to/generate-responses](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/generate-responses) |
| Agent Service | [learn.microsoft.com/azure/foundry/agents/concepts/workflow](https://learn.microsoft.com/azure/foundry/agents/concepts/workflow) |
| Tracing | [learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-client-side](https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-client-side) |
| Evaluations | [learn.microsoft.com/azure/foundry/observability/how-to/evaluate-agent](https://learn.microsoft.com/azure/foundry/observability/how-to/evaluate-agent) |
