# Module 3: Foundry IQ & Agentic RAG (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 2 complete (Contoso Estimator agent exists with File Search + Code Interpreter)

---

## Objective

Extend the Contoso Estimator agent with a **Foundry IQ knowledge base** backed by Azure AI Search, enabling enterprise-grade retrieval across historical project data stored in Azure Blob Storage.

---

## Topics

### 3.1 RAG Progression: File Search → Foundry IQ

| Feature | File Search (Module 2) | Foundry IQ (This Module) |
|---------|:---:|:---:|
| Setup complexity | Low (upload files) | Medium (create KB + sources) |
| Data source | Files uploaded to agent | Azure Blob, SQL, SharePoint, OneLake, Web |
| Multi-source | ❌ One file store per agent | ✅ Multiple sources in one KB |
| Agentic retrieval | ❌ Single query | ✅ Query decomposition + parallel subqueries |
| Reusable across agents | ❌ Per-agent | ✅ One KB serves many agents |
| ACL enforcement | ❌ | ✅ Permission-aware results |
| Citation quality | Basic | Rich (source + page + section) |

### 3.2 What is Foundry IQ?

Foundry IQ is a managed knowledge layer that turns enterprise data into reusable, permission-aware knowledge bases for AI agents.

**Architecture:**
```
Foundry IQ Knowledge Base (on Azure AI Search)
  │
  ├── Knowledge Source 1: Azure Blob (project history PDFs)  ← INDEXED
  ├── Knowledge Source 2: Azure SQL (rate tables)            ← INDEXED
  ├── Knowledge Source 3: SharePoint (policies)              ← INDEXED
  ├── Knowledge Source 4: Web / Bing (live pricing)          ← REMOTE
  └── Knowledge Source N: ...
  │
  └── Agentic Retrieval Engine
        1. Decompose query → subqueries
        2. Select relevant sources
        3. Execute in parallel (keyword/vector/hybrid)
        4. Semantic reranking across all results
        5. Return unified response + citations
```

### 3.3 Knowledge Source Types

| Kind | Description | Indexed or Remote |
|------|-------------|:-:|
| Azure Blob | Auto-generates indexer from blob container | Indexed |
| Azure SQL | From SQL table or view (preview) | Indexed |
| File upload | Direct upload to AI Search | Indexed |
| OneLake | From Fabric lakehouse | Indexed |
| SharePoint (indexed) | Generates indexer from SharePoint site | Indexed |
| SharePoint (remote) | Query-time retrieval | Remote |
| Web (Bing) | Real-time grounding from web | Remote |
| Fabric Data Agent | Answers from Fabric data agent | Remote |
| MCP Server | External MCP server | Remote |

### 3.4 Agentic Retrieval (Key Differentiator)

Unlike simple RAG (single query → single search), agentic retrieval uses an LLM to:
1. **Plan** — decompose complex questions into subqueries
2. **Select** — choose which knowledge sources are relevant (not all every time)
3. **Execute** — run subqueries in parallel across selected sources
4. **Rerank** — semantically rerank combined results
5. **Synthesize** — return a unified response with citations

This produces ~36% higher response quality on complex questions compared to single-shot RAG.

### 3.5 Connection Architecture

```
Agent → Foundry IQ (via portal or MCPTool in SDK)
  │
  └── Knowledge Base (on AI Search)
        └── knowledge_base_retrieve tool (MCP protocol)
              └── Agentic retrieval engine
                    └── Knowledge sources
```

- **Portal path:** Knowledge section → Connect to Foundry IQ (platform handles MCP internally)
- **SDK path:** Explicit `MCPTool` class pointing to KB endpoint (Module 11, Step 3)

---

## Demo: Connect Foundry IQ to the Estimator Agent

### Pre-Demo Setup Checklist

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Foundry resource + project exist | From Module 1 | Project visible |
| 2 | AI Search service provisioned | Azure Portal → Create AI Search (Basic tier) | Service running |
| 3 | Storage account created | Azure Portal → Create Storage Account | Account exists |
| 4 | Blob container `project-history` created | Storage → Containers → New | Container visible |
| 5 | Upload 3 project PDFs | Portal upload or `az storage blob upload-batch` | Files visible in container |
| 6 | Agent exists | From Module 2 | `contoso-estimator-advisor` in portal |

### Demo Steps

**Step 1: Create Knowledge Base in Foundry Portal**

1. Navigate to **Build** → **Knowledge**
2. Click **Connect to Foundry IQ**
3. Connect to your AI Search service (or create new)
4. Create knowledge base: `contoso-estimator-kb`
5. Select embedding model: `text-embedding-3-small`
6. Select chat completions model: `gpt-4-1`

**Step 2: Add Blob Knowledge Source**

1. On the knowledge base page, click **Add knowledge source**
2. Select **Azure Blob Storage**
3. Configure:
   - Name: `ks-project-history`
   - Description: "Historical project close-out reports with final costs, rates, and lessons learned"
   - Storage account: select your account
   - Container: `project-history`
   - Authentication: API Key (or Managed Identity for production)
   - Content extraction mode: Minimal
4. Click **Create**
5. Wait for indexing to complete (green status)

**Step 3: (Optional) Add Web Knowledge Source**

1. Click **Add knowledge source** again
2. Select **Web**
3. This adds Bing-based real-time grounding for live market pricing
4. No configuration needed beyond enabling it

**Step 4: Connect KB to Agent**

1. Navigate to **Build** → **Agents** → select `contoso-estimator-advisor`
2. In the **Knowledge** section, click **Add** → **Connect to Foundry IQ**
3. Select the connection and knowledge base: `contoso-estimator-kb`
4. Save the agent

**Step 5: Test Agentic Retrieval**

Test queries that require cross-document retrieval:

```
Compare earthworks unit rates across our last 3 road projects.
What was the cost variance on the M7 motorway project?
What lessons learned about concrete supply were noted on Project Harbour Bridge?
```

Observe:
- Citations with source document + section
- Multi-document synthesis (agent pulls from multiple PDFs)
- Contrast with File Search (which only searches agent-uploaded files)

---

## Key Takeaways

1. **Foundry IQ** = managed knowledge base on Azure AI Search + knowledge sources + agentic retrieval
2. One KB can have **multiple sources** (Blob, SQL, SharePoint, Web) — reusable across agents
3. **Agentic retrieval** decomposes complex queries into subqueries for better results
4. Portal handles the MCP connection automatically — no code needed
5. For enterprise: add ACL enforcement, sensitivity labels, and scheduled refresh

---

## Full Architecture Vision (Post-Workshop)

For a production Contoso Estimator deployment:

| Knowledge Source | Azure Storage | Purpose |
|---|---|---|
| Project history | Blob Storage (PDFs) | Past bids, close-out reports, lessons learned |
| Rate library | Azure SQL | Structured rate lookup with metadata filtering |
| Standards & specs | Blob Storage (separate container) | AS/NZS standards documents |
| Company policies | SharePoint | Margin guidelines, approval matrix |
| Live pricing | Web (Bing) | Current material market rates |
| Subcontractor quotes | File upload | Per-tender ephemeral documents |

---

## References

| Resource | Link |
|----------|------|
| What is Foundry IQ? | https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq |
| Connect Agents to Foundry IQ | https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect |
| Knowledge Source Types | https://learn.microsoft.com/azure/search/agentic-knowledge-source-overview |
| Agentic Retrieval Overview | https://learn.microsoft.com/azure/search/agentic-retrieval-overview |
| Tutorial (similar pattern) | https://microsoftlearning.github.io/mslearn-ai-agents/Instructions/Exercises/04-integrate-agent-with-foundry-iq.html |
