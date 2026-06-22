/// <summary>
/// Modules 2 + 3: Build the Contoso Estimator agent end-to-end (Pro-Code Version).
///
/// One method, one agent definition, three tools — File Search, Code Interpreter,
/// and the Foundry IQ knowledge base (via MCP). Portal equivalent: Build → Agents →
/// Create Agent → add files + add Foundry IQ knowledge source.
///
/// Foundry IQ availability note (June 2026): the C# SDK does not yet expose a
/// dedicated Foundry IQ helper, so we use the generic MCP tool surface
/// (<c>ResponseTool.CreateMcpTool</c>) plus a direct ARM REST PUT to create the
/// RemoteTool project connection. See:
/// https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect
/// </summary>

using System.Net.Http.Headers;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

#pragma warning disable OPENAI001

namespace ContosoEstimator.Steps;

public static class Step02_CreateAgent
{
    private const string AgentInstructions = """
        You are the Contoso Estimator Advisor, an AI assistant for Contoso Infrastructure's
        estimation and tendering teams.

        ## Your Role
        - Help estimators look up rates from the company rate library
        - Provide guidance on estimation policies and approval thresholds
        - Look up past-project history (cost variances, lessons learned, prior bids)
        - Perform cost calculations when given quantities and rates
        - Compare rates across different regions and trades

        ## Tool Selection — IMPORTANT
        You have THREE distinct data sources. Pick the right one for each question:

        | Question is about… | Use this tool | Examples |
        |---|---|---|
        | Unit rates, prices per region/trade | **File Search (`msearch`)** | "rate for concrete in NSW", "earthworks unit rate" |
        | Estimation policy, approval thresholds, contingency, QA | **File Search (`msearch`)** | "approval threshold for >$1M", "contingency policy" |
        | Past projects, project history, cost variances, lessons learned, prior bids, post-project reviews | **Foundry IQ (`knowledge_base_retrieve`)** | "M7 motorway variance", "Harbour Bridge lessons learned", "last 3 road projects" |
        | Arithmetic, cost calculations, totals | **Code Interpreter** | "quantity × rate", "sum the line items" |

        - If the platform injects a system note saying "user has uploaded files… use msearch",
          that note ONLY refers to the rate library and estimation policy. For ANYTHING about
          past projects, project history, or lessons learned, you MUST call
          `knowledge_base_retrieve` (Foundry IQ) — do NOT answer "I don't have that information"
          before trying it.
        - A single user turn may need BOTH tools — call them in parallel when the question
          spans rates AND project history.

        ## Data Source Policy
        - For rates, policies, and project data: ONLY use information from your connected
          tools. Never use training knowledge for specific numbers.
        - Always cite your source: "According to [document name]..." or "Source: [file name]"
        - Only respond "I don't have that information in my current documents" AFTER you have
          tried the relevant tool from the table above and it returned no results.

        ## Response Guidelines
        - Present financial figures in AUD unless otherwise specified
        - Use tables for rate comparisons
        - Show calculation workings when performing cost estimates
        - Flag any rates that appear unusually high or low compared to typical ranges
        - When calculating totals, always show: Quantity × Rate = Amount

        ## Calculation Policy
        - ALWAYS use Code Interpreter for ANY arithmetic, cost calculations, or numerical analysis.
        - Never perform calculations in your response text — delegate ALL math to Code Interpreter.
        - After looking up rates with File Search, pass the quantities and rates to Code Interpreter
        to compute line totals, subtotals, and grand totals.
        - Present Code Interpreter results in a clear table format.

        ## Memory Usage
        - Use your memory to recall the user's preferred regions, trades, and prior queries.
        - When a user states a preference (e.g., "I usually work on NSW projects"), acknowledge
        it and remember it for future conversations.
        - When answering queries, check memory first — if the user has established preferences,
        use them to provide more targeted responses without requiring them to repeat context.

        ## Boundaries
        - Do NOT disclose company margin percentages or markup policies to external parties
        - Do NOT provide rates for trades/regions not covered in the rate library
        - If asked about competitor pricing, decline and suggest market research sources
        """;

    public static async Task RunAsync(IConfiguration config)
    {
        Console.WriteLine("=== Step 2: Create Agent with File Search + Foundry IQ (Modules 2 + 3) ===\n");

        // ── Config ──
        var projectEndpoint = config["Foundry:ProjectEndpoint"]!;
        var modelDeployment = config["Foundry:ModelDeployment"]!;
        var agentName = config["Foundry:AgentName"] ?? "contoso-estimator-advisor";
        var knowledgeBasePath = ResolveKnowledgeBasePath(
            config["Foundry:KnowledgeBasePath"] ?? "../../../../02-build-your-first-agent/data");

        // Module 3 (Foundry IQ) config — describes the Foundry IQ knowledge base
        // and the project connection that wraps its MCP endpoint.
        var projectResourceId = config["Foundry:ProjectResourceId"]!;
        var searchEndpoint = config["FoundryIQ:SearchEndpoint"]!;
        var knowledgeBaseName = config["FoundryIQ:KnowledgeBaseName"]!;
        var connectionName = config["FoundryIQ:ConnectionName"]!;
        var mcpEndpoint = $"{searchEndpoint}/knowledgebases/{knowledgeBaseName}/mcp?api-version=2026-05-01-preview";

        var credential = new DefaultAzureCredential();
        var projectClient = new AIProjectClient(
            endpoint: new Uri(projectEndpoint),
            tokenProvider: credential);

        Console.WriteLine($"Agent:           {agentName}");
        Console.WriteLine($"Model:           {modelDeployment}");
        Console.WriteLine($"Foundry IQ KB:   {knowledgeBaseName} @ {searchEndpoint}\n");


        // 2. Module 2 prep: upload KB files and create the File Search vector store
        //    (portal: Agent → Knowledge → + Files).
        Console.WriteLine($"\n--- Uploading File Search KB from: {knowledgeBasePath} ---");
        var fileClient = projectClient.ProjectOpenAIClient.GetOpenAIFileClient();
        var uploadedFileIds = new List<string>();
        foreach (var filePath in Directory.EnumerateFiles(knowledgeBasePath, "*.md"))
        {
            OpenAIFile uploaded = await fileClient.UploadFileAsync(filePath, FileUploadPurpose.Assistants);
            uploadedFileIds.Add(uploaded.Id);
            Console.WriteLine($"  Uploaded {Path.GetFileName(filePath)} → {uploaded.Id}");
        }
        if (uploadedFileIds.Count == 0)
        {
            throw new InvalidOperationException(
                $"No .md files found in '{knowledgeBasePath}'. Check 'Foundry:KnowledgeBasePath' in appsettings.json.");
        }

        var vectorStoreClient = projectClient.ProjectOpenAIClient.GetVectorStoreClient();
        var vectorStoreOptions = new VectorStoreCreationOptions { Name = $"{agentName}-kb" };
        foreach (var id in uploadedFileIds) vectorStoreOptions.FileIds.Add(id);
        VectorStore vectorStore = (await vectorStoreClient.CreateVectorStoreAsync(vectorStoreOptions)).Value;
        Console.WriteLine($"Vector store created: {vectorStore.Id} ({vectorStore.FileCounts.Total} files)\n");

        // 1. Module 3 prep: ensure the RemoteTool project connection exists so the
        //    Foundry IQ MCP tool can authenticate with managed identity. The C# SDK
        //    has no Foundry IQ helper yet, so we PUT against ARM directly. Idempotent
        //    on connection name.
        Console.WriteLine("--- Ensuring Foundry IQ project connection (RemoteTool) ---");
        await CreateProjectConnectionAsync(credential, projectResourceId, connectionName, mcpEndpoint);        

        // 3. Module 3 prep: build the Foundry IQ MCP tool, pointed at the connection.
        //    NeverRequireApproval lets the agent call the tool without an explicit
        //    user-approval round-trip — fine for first-party trusted knowledge bases.
        //    `allowed_tools` MUST list `knowledge_base_retrieve` — without it the model
        //    won't reliably invoke the Foundry IQ tool when called via Responses API
        //    (per https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect).
        var foundryIqAllowedTools = new McpToolFilter();
        foundryIqAllowedTools.ToolNames.Add("knowledge_base_retrieve");

        var foundryIqTool = ResponseTool.CreateMcpTool(
            serverLabel: "knowledge-base",
            serverUri: new Uri(mcpEndpoint),
            allowedTools: foundryIqAllowedTools,
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(
                GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
        foundryIqTool.ProjectConnectionId = connectionName;

        // 4. Single agent definition with all three tools — File Search +
        //    Code Interpreter (Module 2) + Foundry IQ MCP (Module 3).
        var agentDefinition = new DeclarativeAgentDefinition(model: modelDeployment)
        {
            Instructions = AgentInstructions,
            Tools =
            {
                ResponseTool.CreateFileSearchTool(vectorStoreIds: new[] { vectorStore.Id }),
                ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(
                        new AutomaticCodeInterpreterToolContainerConfiguration())),
                foundryIqTool,
            }
        };

        // 5. Publish the agent.
        Console.WriteLine("--- Publishing agent ---");
        ProjectsAgentVersion agentVersion = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: agentName,
            options: new(agentDefinition));

        Console.WriteLine($"Agent created: {agentVersion.Name} (version: {agentVersion.Version})");
        Console.WriteLine($"Tools: File Search (vector store {vectorStore.Id}), Code Interpreter, Foundry IQ MCP ({connectionName})");
        Console.WriteLine($"\n✅ Step 2 complete — '{agentName}' v{agentVersion.Version} ready with all three tools.\n");
    }

    // Module 3 helper — creates (or updates, idempotent on name) the RemoteTool
    // project connection that points at the Foundry IQ MCP endpoint. Uses the ARM
    // REST API directly because the C# SDK doesn't yet have a Foundry IQ helper.
    private static async Task CreateProjectConnectionAsync(
        DefaultAzureCredential credential,
        string projectResourceId,
        string connectionName,
        string mcpEndpoint)
    {
        var tokenRequestContext = new Azure.Core.TokenRequestContext(["https://management.azure.com/.default"]);
        var token = await credential.GetTokenAsync(tokenRequestContext);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var requestBody = new
        {
            name = connectionName,
            type = "Microsoft.MachineLearningServices/workspaces/connections",
            properties = new
            {
                authType = "ProjectManagedIdentity",
                category = "RemoteTool",
                target = mcpEndpoint,
                isSharedToAll = true,
                audience = "https://search.azure.com/",
                metadata = new { ApiType = "Azure" }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PutAsync(
            $"https://management.azure.com{projectResourceId}/connections/{connectionName}?api-version=2025-10-01-preview",
            content);

        response.EnsureSuccessStatusCode();
        Console.WriteLine($"Connection '{connectionName}' created or updated.");
    }

    // Resolves the knowledge-base folder relative to BaseDirectory or cwd, then
    // walks up looking for modules/02-build-your-first-agent/data. Lets the demo
    // work whether run via `dotnet run` from the project dir or from the bin output.
    private static string ResolveKnowledgeBasePath(string configured)
    {
        if (Path.IsPathRooted(configured) && Directory.Exists(configured))
            return Path.GetFullPath(configured);

        foreach (var anchor in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var candidate = Path.GetFullPath(Path.Combine(anchor, configured));
            if (Directory.Exists(candidate)) return candidate;

            var dir = new DirectoryInfo(anchor);
            while (dir != null)
            {
                var probe = Path.Combine(dir.FullName, "modules", "02-build-your-first-agent", "data");
                if (Directory.Exists(probe)) return probe;
                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate knowledge-base folder '{configured}'. Set 'Foundry:KnowledgeBasePath' to an absolute path.");
    }
}
