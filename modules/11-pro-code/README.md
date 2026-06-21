# Module 11: Pro-Code Development (.NET) (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Modules 1-2 complete (Foundry resource + agent exist)

---

## Objective

Demonstrate the programmatic equivalent of every portal step from Modules 1-7 using a single .NET solution with progressive, uncommentable steps. The presenter walks through the code showing "this is what the portal did behind the scenes."

---

## Concept

This module uses a **single .NET console application** with 8 step files. Each step mirrors a previous portal-led module. The presenter uncomments one step at a time in `Program.cs` to show the SDK equivalent.

```
modules/11-pro-code/src/ContosoEstimator/
├── ContosoEstimator.csproj
├── Program.cs                          # Main — uncomment one step at a time
├── Steps/
│   ├── Step01_FirstApiCall.cs          # Module 1: Call GPT-5.4
│   ├── Step02_CreateAgent.cs           # Module 2: Create agent + File Search
│   ├── Step03_FoundryIQ.cs             # Module 3: MCPTool + KB
│   ├── Step04_ContentSafety.cs         # Module 4: Guardrails
│   ├── Step05_AddTools.cs              # Module 5: Code Interpreter + OpenAPI
│   ├── Step06_Tracing.cs              # Module 6: OpenTelemetry
│   ├── Step07_Evaluation.cs            # Module 7: Batch eval
│   ├── Step07b_ContinuousEvaluation.cs # Module 7: Continuous eval rule
│   └── Step08_StreamingChat.cs         # Bonus: Console streaming chat
├── appsettings.json
└── .env.example
```

### Web Chat UI (Production Pattern)

```
modules/11-pro-code/src/ContosoEstimator.Web/
├── ContosoEstimator.Web.csproj         # ASP.NET Core web project
├── Program.cs                          # Minimal API + SSE streaming backend
├── appsettings.json                    # Configuration (ProjectEndpoint, AgentName)
└── wwwroot/
    ├── index.html                      # Chat UI (single-page app)
    ├── styles.css                      # Fluent-inspired styling
    └── chat.js                         # SSE streaming client
```

**Run the web chat:**

```bash
cd modules/11-pro-code/src/ContosoEstimator.Web
# Set your endpoint in appsettings.json or via env var
export AZURE_AI_PROJECT_ENDPOINT="https://your-project.services.ai.azure.com/api/projects/your-project"
dotnet run
# Browse to http://localhost:5050
```

The web client demonstrates:
- ASP.NET Core minimal API as a thin proxy to Foundry Agent Service
- Server-Sent Events (SSE) for real-time token streaming
- Conversation management (create, multi-turn context)
- MCP tool approval handling
- Responsive chat UI with markdown rendering

---

## Demo Flow

1. Show `Program.cs` with all steps commented
2. Uncomment **Step 1** → run → first API call works
3. Uncomment **Step 2** → run → agent created with File Search
4. Walk through **Step 3** code (MCPTool) — explain this is what portal did
5. Skip to **Step 8** → run → full streaming chat UI
6. Time permitting: show **Step 7** (evaluation) for CI/CD story

---

## Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `Azure.AI.Projects` (2.x) | Foundry SDK — agents, evaluations |
| `Azure.Identity` | DefaultAzureCredential |
| `Azure.Monitor.OpenTelemetry.AspNetCore` | Tracing |
| `OpenTelemetry.Extensions.Hosting` | Custom spans |
| `Microsoft.Extensions.Configuration` | Config management |

---

## Step-to-Module Mapping

| Step | Portal Module | What the Code Does | Key Class/Method |
|------|:---:|--------------------|----|
| 01 | 1 | Call chat completions | `OpenAI()` + `base_url` |
| 02 | 2 | Create agent + upload files + File Search | `AIProjectClient.Agents.CreateVersion()` || 02b | 2 | Create memory store + attach Memory Search tool | `MemoryStoreDefaultDefinition` + `MemorySearchPreviewTool` || 03 | 3 | Create MCPTool pointing to KB endpoint | `MCPTool` + `RemoteTool` connection |
| 04 | 4 | Create guardrail + assign to agent | Content Safety SDK |
| 05 | 5 | Add Code Interpreter + OpenAPI tool | `ToolDefinition` classes |
| 06 | 6 | Add OpenTelemetry traces + custom spans | `Azure.Monitor.OpenTelemetry` |
| 07 | 7 | Load eval dataset + run batch eval | `AIProjectClient.Evaluations` |
| 07b | 7 | Create continuous evaluation rule | `EvaluationRule` + `EvaluationRuleEventType` |
| 08 | Bonus | ASP.NET Core minimal API + SSE streaming | Responses API + streaming |

---

## References

| Resource | Link |
|----------|------|
| SDK Overview | https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview |
| .NET SDK Reference | https://learn.microsoft.com/dotnet/api/overview/azure/ai.projects-readme |
| Quickstart Code | https://learn.microsoft.com/azure/foundry/quickstarts/get-started-code |
| Responses API | https://learn.microsoft.com/azure/foundry/foundry-models/how-to/generate-responses |
