# Step 2 · Rate Library on Azure SQL (Foundry IQ Knowledge Source)

> **📍 Step 2 of 4** · [🧭 Overview](00-foundry-iq-azure-sql-rls.md) · ⬅ Prev: [Step 1 · Setup](01-foundry-iq-azure-sql-setup.md) · ➡ Next: [Step 3 · Track 1](03-foundry-iq-rls-security-filter.md)

**Version:** 1.0
**Last Updated:** July 2026
**Format:** Led Demo + pro-code snippet
**Prerequisite:** [Demo resource setup](01-foundry-iq-azure-sql-setup.md) complete

> Deep-dive companion to [Module 3](../README.md). Where the main module indexes
> **unstructured** project history from Blob storage, this guide serves the
> **structured** rate library from **Azure SQL** as a Foundry IQ knowledge source.

---

## Objective

Serve the Contoso rate library from an **Azure SQL knowledge source**
(`kind = indexedSql`, preview) and connect it to a variant of the Contoso
Estimator agent — so rate lookups come from a governed SQL table instead of an
uploaded document.

---

## Why SQL instead of File Search for rates?

| Question | File Search / Blob | Azure SQL knowledge source |
|----------|:---:|:---:|
| "What is the NSW 40MPa concrete rate?" | Approximate (text match) | Exact (row lookup) |
| "List every NSW rate under $50/unit" | ❌ Hard | ✅ Filterable field |
| "Compare formwork across all regions" | Partial | ✅ Precise per-region rows |
| Row-level access control | ❌ | ✅ (see the two RLS tracks) |
| Source of truth already relational | Copy to a doc | ✅ Query the table directly |

**Rule of thumb:** structured, tabular, frequently-updated lookups → Azure SQL.
Narrative documents (close-out reports, policies) → Blob / File Search.

---

## How the Azure SQL knowledge source works

```
Azure SQL table (dbo.rate_library)
   │  each ROW = one logical document
   ▼
Azure SQL knowledge source (kind: indexedSql)
   │  auto-generates: data source → skillset* → indexer → index   (*if embeddings)
   ▼
Knowledge base (Foundry IQ)  ── MCP: knowledge_base_retrieve ──►  Agent
```

Key facts (verified against
[Learn](https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-azure-sql)):

- Ingests **one** table or view; each row becomes one logical document.
- Requires a **single-valued primary key** (auto-discovered) — our `rate_id`.
- `contentExtractionMode` must be **`minimal`** (row-based, no file parsing).
- Ingestion is **schedule-based** (no real-time sync); enable SQL change
  tracking for incremental updates.
- Column mapping: `contentColumns` become searchable text fields;
  `embeddingColumns` generate vectors (needs an embedding model).

---

## Demo A — Create the knowledge source (Foundry portal)

### Pre-demo checklist

| # | Task | Verify |
|---|------|--------|
| 1 | [Setup](01-foundry-iq-azure-sql-setup.md) complete | 156 rows in `dbo.rate_library` |
| 2 | Search service + managed identity | `az search service show` |
| 3 | `text-embedding-3-small` deployed | Foundry → Build → Models |
| 4 | RBAC propagated | Role assignments visible |

### Steps

1. In the Foundry portal → **Build** → **Knowledge** → **Connect to Foundry IQ**.
2. Select your Azure AI Search service.
3. **Add knowledge source** → **Azure SQL Database (preview)**.
4. Configure:
   - **Name:** `ks-rate-library-sql`
   - **Description:** `Contoso unit rate library by trade, region and division. Use for exact rate lookups and regional rate comparisons.`
   - **Connection:** managed identity (recommended) or a connection string.
   - **Table:** `dbo.rate_library`
   - **Content columns:** `content_text`, `trade_item`, `category`, `division`, `region`, `unit`, `rate_aud`, `owner_group`
   - **Embedding column:** `content_text` → `text-embedding-3-small`
   - **Content extraction mode:** `minimal`
5. **Create** and wait for ingestion to reach a green/succeeded state.
6. Create a knowledge base `contoso-estimator-kb-sql` that references
   `ks-rate-library-sql`.

> The description is important: at `low`/`medium` reasoning effort the retrieval
> engine uses it to decide when to query this source.

---

## Demo B — Variant agent that uses SQL for rates

Create a **new** agent (leave the Module 2 agent untouched):

| Setting | Value |
|---------|-------|
| Agent name | `contoso-estimator-advisor-sql` |
| Model | `gpt-5-4` |
| Tools | **Foundry IQ** (`contoso-estimator-kb-sql`), **Code Interpreter** |
| File Search | Policy document **only** — *do not* upload the rate library |

System-prompt delta (add to the Module 2 prompt):

```
## Data Source Policy (rates)
- Look up ALL unit rates via the Foundry IQ knowledge base (knowledge_base_retrieve).
  The rate library lives in Azure SQL, not in uploaded files.
- Never guess or reuse a rate from a different region. If the region is unclear, ask.
- Cite the trade item, region, unit and effective date from the retrieved row.
```

### Test queries (portal playground)

```
What is the NSW rate for concrete supply & pour (40MPa)?
List all NSW rates under $50 per unit.
Compare the formwork (standard) rate across NSW, VIC, QLD and WA.
```

Expected: exact per-row answers with region + unit + effective date, sourced
from `ks-rate-library-sql` (visible in the retrieval activity).

> ⚠️ This baseline agent returns **all** regions' rates to every user. The two
> RLS tracks add per-user restrictions on top.

---

## Pro-code: create the knowledge source in .NET

Mirrors the Module 11 pattern. Package: `Azure.Search.Documents` (prerelease).

```csharp
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases.Models;

var indexClient = new SearchIndexClient(new Uri(searchEndpoint), new DefaultAzureCredential());

var ingestion = new KnowledgeSourceIngestionParameters
{
    ContentExtractionMode = "minimal",
    EmbeddingModel = new KnowledgeSourceAzureOpenAIVectorizer
    {
        AzureOpenAIParameters = new AzureOpenAIVectorizerParameters
        {
            ResourceUri = new Uri(foundryEndpoint),
            DeploymentName = "text-embedding-3-small",
            ModelName = "text-embedding-3-small"
        }
    }
};

var sqlParams = new IndexedSqlKnowledgeSourceParameters(
    connectionString: sqlConnectionString,   // or use a managed-identity connection string
    tableOrView: "dbo.rate_library")
{
    ContentColumns =
    {
        new ContentColumnMapping("content_text", "content_text", "Edm.String"),
        new ContentColumnMapping("trade_item",  "trade_item",  "Edm.String"),
        new ContentColumnMapping("category",    "category",    "Edm.String"),
        new ContentColumnMapping("division",    "division",    "Edm.String"),
        new ContentColumnMapping("region",      "region",      "Edm.String"),
        new ContentColumnMapping("unit",        "unit",        "Edm.String"),
        new ContentColumnMapping("owner_group", "owner_group", "Edm.String"),
    },
    EmbeddingColumns = { new EmbeddingColumnMapping("content_vector", "content_text") },
    IngestionParameters = ingestion
};

var knowledgeSource = new IndexedSqlKnowledgeSource(
    name: "ks-rate-library-sql",
    indexedSqlParameters: sqlParams)
{
    Description = "Contoso unit rate library by trade, region and division."
};

await indexClient.CreateOrUpdateKnowledgeSourceAsync(knowledgeSource);
```

Wire the knowledge base into the agent with the same `CreateMcpTool` /
`knowledge_base_retrieve` pattern shown in
[Module 11 Step02_CreateAgent.cs](../../11-pro-code/src/ContosoEstimator/Steps/Step02_CreateAgent.cs).

---

## Next: row-level access control

The base agent shows the full rate library to everyone. To restrict rows per
user (e.g. a NSW estimator only sees NSW rates):

| Track | Enforcement | Availability | Guide |
|-------|-------------|--------------|-------|
| **1 — Security filter** | App-mediated OData filter | GA | [03-foundry-iq-rls-security-filter.md](03-foundry-iq-rls-security-filter.md) |
| **2 — Identity RLS** | Entra token + permission filters | Preview | [04-foundry-iq-rls-identity.md](04-foundry-iq-rls-identity.md) |

> **Important:** the Azure SQL knowledge source has **no** native permission
> enforcement, and Azure SQL's own Row-Level Security does **not** flow through
> the indexer (it reads all rows under one identity). Row-level access is
> enforced at the **search index / retrieval** layer — see the two tracks.

---

## Key takeaways

1. Azure SQL knowledge source = one table, one row per document, single-valued PK.
2. Great for **exact** and **filterable** rate lookups; keep documents in Blob.
3. `contentExtractionMode` must be `minimal`; embeddings are optional but enable
   semantic ranking.
4. The variant agent removes the rate library from File Search and grounds rates
   in SQL instead.
5. Per-user restrictions are added at the retrieval layer (Tracks 1 and 2).

---

## References

| Resource | Link |
|----------|------|
| Azure SQL knowledge source (preview) | https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-azure-sql |
| Knowledge source overview | https://learn.microsoft.com/azure/search/agentic-knowledge-source-overview |
| Create a knowledge base | https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-create-knowledge-base |
| Query a knowledge base / MCP | https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-retrieve |
| Connect agents to Foundry IQ | https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect |
