# Foundry IQ & Azure SQL Integration with Row-Level Access

> **🧭 Overview / start here — this file is the map, not a step.**
> Do the steps in this order:
> **[Step 1 · Setup](01-foundry-iq-azure-sql-setup.md)** → **[Step 2 · SQL knowledge source + agent](02-foundry-iq-azure-sql.md)** → then RLS: **[Step 4 · Track 2 — Identity RLS (recommended, working)](04-foundry-iq-rls-identity.md)**. [Step 3 · Track 1](03-foundry-iq-rls-security-filter.md) is a **concept-only reference** (no sample code).
>
> **Bottom line:** the one working, secure, identity-native option is **Track 2** —
> the app forwards the signed-in user's Entra token and Azure AI Search trims rows
> by permission filter. Track 1 (app-set OData filter) is documented for contrast
> only; it is app-layer, not identity-native, and **cannot be delegated to a
> Foundry agent**.

**Version:** 1.0
**Last Updated:** July 2026
**Format:** Optional deep-dive for [Module 3](../README.md)
**Prerequisite:** Module 2 complete (Contoso Estimator agent exists)

> Module 3's main demo indexes **unstructured** project history from Blob storage.
> This deep-dive serves the **structured** rate library from **Azure SQL** as a
> Foundry IQ knowledge source, then adds per-user **row-level access control (RLS)**
> so an estimator only sees rates for their region.

---

## Objective

Integrate **Azure SQL** with **Foundry IQ** as an indexed knowledge source, then
enforce **row-level access** so the Contoso Estimator agent returns only the rate
rows each user is allowed to see (e.g. a NSW estimator sees only NSW rates).

---

## Why Azure SQL for the rate library?

| Question | File Search / Blob | Azure SQL knowledge source |
|----------|:---:|:---:|
| "What is the NSW 40MPa concrete rate?" | Approximate (text match) | Exact (row lookup) |
| "List every NSW rate under $50/unit" | ❌ Hard | ✅ Filterable field |
| "Compare formwork across all regions" | Partial | ✅ Precise per-region rows |
| Row-level access control | ❌ | ✅ (the two RLS tracks below) |

Structured, tabular, frequently-updated lookups → **Azure SQL**. Narrative
documents (close-out reports, policies) → Blob / File Search.

---

## Architecture

```
Azure SQL table (dbo.rate_library)          Microsoft Entra
   │  each ROW = one logical document          │ users + groups
   ▼                                            │
Foundry IQ knowledge source (indexedSql)        │
   │  generated: data source → indexer → index  │
   ▼                                            ▼
Knowledge base ── MCP: knowledge_base_retrieve ──► Agent / consuming app
   │
   └── Row-level access enforced at the RETRIEVAL layer:
         • Track 2 — Entra token + index permission filters  [recommended, working]
         • Track 1 — app-set OData filterAddOn (region)       [concept-only, no code]
```

> **Important:** the Azure SQL knowledge source has **no native permission
> enforcement**, and SQL's own Row-Level Security does **not** propagate through
> the indexer (it reads every row under one identity). Row-level access is enforced
> at the **search/retrieval** layer — see the two tracks.

---

## Guides in this deep-dive

Work through them in order.

| # | Guide | What it covers |
|---|-------|----------------|
| 1 | [Demo resource setup](01-foundry-iq-azure-sql-setup.md) | Provision Azure SQL, AI Search, RBAC, Entra groups + app registration |
| 2 | [Rate library on Azure SQL](02-foundry-iq-azure-sql.md) | Create the `indexedSql` knowledge source + a SQL-backed variant agent (unfiltered baseline) |
| 3 | [Track 1 — Security filter (reference only)](03-foundry-iq-rls-security-filter.md) | Concept: app-mediated `filterAddOn` — **no sample code** (not identity-native; can't run through an agent) |
| 4 | [Track 2 — Identity RLS (recommended)](04-foundry-iq-rls-identity.md) | Entra permission filters + user-token trimming + [.NET web app](../src/track2-identity-rls) |

Supporting asset: [data/rate-library.sql](../data/rate-library.sql) migrates the
Module 2 markdown rate library into a normalized Azure SQL table
(156 rows with `region`, `division`, `owner_group`).

---

## Why two RLS tracks?

Row-level access must be enforced at the search/retrieval layer. **Only one option
is shipped and recommended — Track 2 (identity-native).** Track 1 is kept as a
conceptual contrast so the trade-off is clear.

| | [Track 1 — Security filter](03-foundry-iq-rls-security-filter.md) | [Track 2 — Identity RLS](04-foundry-iq-rls-identity.md) |
|---|---|---|
| **Status** | 📖 Concept-only reference (no code) | ✅ **Recommended, working** |
| **Enforcement** | App builds an OData `filterAddOn` from the user's region | Azure AI Search trims rows from the user's Entra token |
| **Trust boundary** | Your app | Azure AI Search + Entra |
| **App writes filter logic?** | ✅ Yes (from verified user claims) | ❌ No |
| **Identity-native?** | ❌ No (app-layer string filter) | ✅ Yes (Entra groups) |
| **Works through a Foundry agent?** | ❌ No (model would own the filter; no per-user injection) | ❌ Not via Agent Service either — see note below |
| **Ingestion** | SQL auto-indexer + wrapped `searchIndex` KS | Push model with permission-tagged rows |
| **Consuming app** | *(none shipped)* | [.NET web + sign-in](../src/track2-identity-rls) |

> **Per-user RLS cannot run through a Foundry Agent Service agent.** The agent
> calls the knowledge base with the **project's** managed identity, and
> [per-request headers for MCP tools aren't supported](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect),
> so the chatting user's token never reaches Search. Identity-native per-user
> trimming therefore requires an **app that calls retrieve directly** (Track 2)
> or the **Azure OpenAI Responses API** — both pass the user token per request.

---

## Why a consuming app (and not the portal playground or an agent)?

Per-user row filtering **cannot** be demonstrated in the Foundry portal playground
or delegated to a Foundry agent:

- **Playground** runs one fixed agent as *you* — no per-user injection point.
- **A Foundry agent** calls the knowledge base with the **project managed
  identity**, not the chatting user's token, and per-request MCP headers aren't
  supported — so Search can't trim by the user's identity.

The portal playground is still used for the **unfiltered baseline** (everyone sees
all rates). Per-user enforcement (Track 2) requires an **app that calls retrieve
directly and forwards the user's token**.

---

## Which should I choose?

**Use Track 2 (identity-native RLS).** It is the one shipped, secure, working
option — enforcement lives in Azure AI Search + Entra, and the app writes no
filter. Track 1 remains only as a conceptual contrast (app-layer filter, no code,
cannot run through an agent).

---

## Key takeaways

1. Azure SQL knowledge source = one table, one row per document, single-valued PK.
2. Great for **exact** and **filterable** rate lookups; keep documents in Blob.
3. The Azure SQL source has **no native permission enforcement** — RLS lives at the
   retrieval layer.
4. **Track 2** (recommended) enforces access in Azure AI Search via Entra
   permission filters + the user's token. **Track 1** (concept only) would enforce
   it in the app via `filterAddOn` — app-layer, not identity-native, no sample code.
5. **Per-user RLS cannot run through a Foundry Agent Service agent** (project MI,
   no per-request token). It needs an app that calls retrieve directly (Track 2)
   or the Azure OpenAI Responses API.
6. The portal playground only shows the unfiltered baseline.

---

## References

| Resource | Link |
|----------|------|
| Azure SQL knowledge source (preview) | https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-azure-sql |
| Knowledge source overview | https://learn.microsoft.com/azure/search/agentic-knowledge-source-overview |
| Query a knowledge base / MCP | https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-retrieve |
| Document-level access control | https://learn.microsoft.com/azure/search/search-document-level-access-overview |
| Connect agents to Foundry IQ | https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect |
