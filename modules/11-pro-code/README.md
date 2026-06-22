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

**Run the step walkthrough**

*Prerequisites*

- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed (`dotnet --version` should report `10.x` — see [ContosoEstimator.csproj](src/ContosoEstimator/ContosoEstimator.csproj))
- Signed in to Azure: `az login` (steps authenticate via `DefaultAzureCredential`)
- **Foundry User** role on the Foundry resource (granted in [Module 1](../01-foundry-setup/README.md))
- Some steps depend on prior modules — e.g. Step 3 expects a Foundry IQ knowledge base, Step 6 expects an App Insights connection string
- .NET SDK ref: [Foundry .NET SDK](https://learn.microsoft.com/en-gb/dotnet/api/overview/azure/ai.projects-readme?view=azure-dotnet-preview)

*1. Configure*

Edit [src/ContosoEstimator/appsettings.json](src/ContosoEstimator/appsettings.json) with your project endpoint, model deployment, knowledge-base name, and App Insights connection string — or copy [.env.example](src/ContosoEstimator/.env.example) to `.env` and export the `Foundry__*`, `FoundryIQ__*`, and `AppInsights__*` variables (double underscores map to the JSON section separators).

```powershell
# Windows PowerShell
$env:Foundry__ProjectEndpoint = "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/contoso-estimator"
```

```bash
# macOS / Linux / WSL
export Foundry__ProjectEndpoint="https://YOUR-RESOURCE.services.ai.azure.com/api/projects/contoso-estimator"
```

*2. Pick a step in `Program.cs`*

Open [src/ContosoEstimator/Program.cs](src/ContosoEstimator/Program.cs). Every step is commented out except `Step08_StreamingChat`. Uncomment **one** step at a time:

```csharp
// Step 1: First API Call
await ContosoEstimator.Steps.Step01_FirstApiCall.RunAsync(config);

// Step 2: Create Agent with File Search
// await ContosoEstimator.Steps.Step02_CreateAgent.RunAsync(config);
```

*3. Restore, build, and run*

```powershell
cd modules/11-pro-code/src/ContosoEstimator
dotnet restore
dotnet build --nologo
dotnet run --no-build
```

> `dotnet run` on its own will implicitly restore + build, but running them as separate steps gives you cleaner output and lets you spot build errors before the app starts. Use `dotnet build -c Release` and `dotnet run -c Release --no-build` for a release-mode demo.

To iterate quickly between steps, leave `dotnet watch run` running and just toggle which line is uncommented — the app rebuilds and re-runs on save:

```powershell
dotnet watch run
```

*Recommended order for a led demo*

| Order | Step | Why |
|---|---|---|
| 1 | `Step01_FirstApiCall` | Proves auth + endpoint are correct |
| 2 | `Step02_CreateAgent` | Creates the agent the rest of the steps (and the web client) reuse |
| 3 | Walk through `Step03`–`Step07` code | Read-through; only run the ones whose dependencies (KB, App Insights, eval dataset) are provisioned |
| 4 | `Step08_StreamingChat` | Console chat against the agent — good lead-in to the Web Chat UI below |

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

**Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download) installed (`dotnet --version` should report `9.x`)
- Signed in to Azure: `az login` (the web app authenticates via `DefaultAzureCredential`)
- A Foundry agent already exists in your project — created in [Module 2](../02-build-your-first-agent/README.md) or by running `Step02_CreateAgent` from the console app. The default name is `contoso-estimator-advisor`.
- Your signed-in identity has the **Foundry User** role on the Foundry resource (granted in [Module 1](../01-foundry-setup/README.md))

**1. Configure the project endpoint**

Pick **one** of the following:

*Option A — edit [appsettings.json](src/ContosoEstimator.Web/appsettings.json) (recommended for demos):*

```jsonc
{
  "Foundry": {
    "ProjectEndpoint": "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/contoso-estimator",
    "AgentName": "contoso-estimator-advisor"
  }
}
```

*Option B — set environment variables (overrides `appsettings.json`):*

```powershell
# Windows PowerShell
$env:AZURE_AI_PROJECT_ENDPOINT = "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/contoso-estimator"
$env:AZURE_AI_AGENT_NAME       = "contoso-estimator-advisor"   # optional
```

```bash
# macOS / Linux / WSL
export AZURE_AI_PROJECT_ENDPOINT="https://YOUR-RESOURCE.services.ai.azure.com/api/projects/contoso-estimator"
export AZURE_AI_AGENT_NAME="contoso-estimator-advisor"          # optional
```

> Grab the **Project endpoint** from the Foundry portal → your project → **Overview** → *Endpoints*, or from the `Foundry__ProjectEndpoint` value in [src/ContosoEstimator/.env.example](src/ContosoEstimator/.env.example).

**2. Restore, build, and run**

```powershell
cd modules/11-pro-code/src/ContosoEstimator.Web
dotnet restore
dotnet build --nologo
dotnet run --no-build
```

> `dotnet run` on its own will implicitly restore + build, but running them as separate steps gives you cleaner output and lets you catch build errors before Kestrel starts. For a production-style demo, use `dotnet build -c Release` and `dotnet run -c Release --no-build`.

You should see:

```text
Now listening on: http://localhost:5050
Application started. Press Ctrl+C to shut down.
```

**3. Open the chat UI**

Browse to **http://localhost:5050** and send a prompt (e.g., *"Estimate scaffolding cost for a 2-story commercial building in Sydney."*). You'll see tokens streaming in real time via SSE.

**Useful endpoints**

| Endpoint | Purpose |
|---|---|
| `GET /api/health` | Confirms config + agent name are wired up |
| `POST /api/conversations` | Creates a new Foundry conversation |
| `POST /api/chat` | SSE streaming chat (body: `{ "message": "...", "conversationId": "..." }`) |
| `DELETE /api/conversations/{id}` | Ends a conversation |

**Troubleshooting**

| Symptom | Fix |
|---|---|
| `InvalidOperationException: Set Foundry:ProjectEndpoint...` | Endpoint isn't configured — repeat step 1 |
| `401 / 403` from Foundry | Run `az login`; confirm **Foundry User** role on the resource |
| `Agent not found` | Create the agent first (Module 2 or `Step02_CreateAgent`) and verify the name matches `Foundry:AgentName` |
| Port `5050` already in use | Change the URL at the bottom of [src/ContosoEstimator.Web/Program.cs](src/ContosoEstimator.Web/Program.cs) (`app.Run("http://localhost:5050")`) |

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
