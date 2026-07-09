# Step 3 · Track 1 — Query-Time Security Filter (reference only, not shipped)

> **📍 Step 3 of 4 · RLS option A — reference/concept only** · [🧭 Overview](00-foundry-iq-azure-sql-rls.md) · ⬅ Prev: [Step 2 · SQL knowledge source](02-foundry-iq-azure-sql.md) · ➡ Recommended: [Step 4 · Track 2 — Identity RLS](04-foundry-iq-rls-identity.md)

**Version:** 2.0
**Last Updated:** July 2026
**Format:** Concept / reference (no sample code)
**Prerequisites:** [Setup](01-foundry-iq-azure-sql-setup.md) + [Base SQL knowledge source](02-foundry-iq-azure-sql.md)

> ⚠️ **This approach is documented for completeness only — no sample code ships
> with the workshop.** It is **not** the recommended way to enforce row-level
> access. Use **[Track 2 — Identity-Native RLS](04-foundry-iq-rls-identity.md)**,
> which enforces access from the user's Microsoft Entra identity inside Azure AI
> Search. This page explains what the security-filter approach is, and — more
> importantly — **why it is not identity-native and cannot be delegated to an
> agent.**

---

## What it is

A query-time **OData filter** (`filterAddOn`) that an application sets on the
knowledge base **retrieve** call, derived server-side from the signed-in user
(e.g. `region eq 'NSW'`). The application — not the model — owns the filter.

```
User (NSW) ─► App derives filter "region eq 'NSW'" ─► KB retrieve(filterAddOn) ─► NSW rows only
User (VIC) ─► App derives filter "region eq 'VIC'" ─► KB retrieve(filterAddOn) ─► VIC rows only
```

`filterAddOn` is a property of `SearchIndexKnowledgeSourceParams`, so it only
applies to a **`searchIndex`** knowledge source — an `indexedSql` source has no
filter parameter. You would wrap the SQL-generated index as a `searchIndex`
knowledge source to use it.

---

## Why it is **not** the recommended approach

1. **It is not identity-native.** Access is a string filter the *app* composes.
   Security depends entirely on the app deriving the right filter from verified
   claims every time. Compare this to Track 2, where Azure AI Search trims rows
   from the user's Entra token and the app writes no filter at all.
2. **It cannot be delegated to a Foundry agent.** When a Foundry IQ knowledge
   base is attached to an agent, the LLM generates the `knowledge_base_retrieve`
   arguments via MCP. Letting the model set the security filter is exactly the
   trust boundary you must never cross, and **Foundry Agent Service doesn't
   support per-request headers/params for MCP tools** — so there is no place to
   inject a per-user filter into the agent's call. (See
   [Connect a Foundry IQ knowledge base to Foundry Agent Service](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect):
   *"headers set in agent definitions apply to all invocations and can't vary by
   user or request… for per-user authorization, use the Azure OpenAI Responses
   API instead."*)
3. **The portal playground can't demonstrate it either** — the playground runs a
   fixed agent as you, with no per-user injection point.

The net effect: a security filter can only be applied by an **app that calls the
retrieve endpoint directly** and owns the filter. That works, but it is an
app-layer control, not identity governance — and it does not give you the
"NSW estimator chats the agent → sees only NSW data" experience. For that you
need identity-native trimming (Track 2) or the Azure OpenAI Responses API.

---

## If you still need an app-layer filter

The pattern (no sample shipped) is:

1. Authenticate the user; derive their region/department from **verified claims**
   (never from the model).
2. Wrap the SQL-generated index (or a custom index) as a **`searchIndex`**
   knowledge source and add it to a knowledge base.
3. Call `KnowledgeBaseRetrievalClient.RetrieveAsync` with
   `SearchIndexKnowledgeSourceParams.FilterAddOn = "region eq '<user region>'"`.
4. Feed the trimmed rows to the model for the answer.

**The app is the trust boundary** — the LLM must never be able to set or widen the
filter. Treat any agent/prompt-level restriction as defense-in-depth only, never
as the control.

---

## Recommended path

Use **[Track 2 — Identity-Native Row-Level Security](04-foundry-iq-rls-identity.md)**.
It tags each row with the Entra group(s) allowed to see it, and Azure AI Search
trims results from the signed-in user's token (`x-ms-query-source-authorization`)
— the app writes no filter, and enforcement lives in Search + Entra.

---

## References

| Resource | Link |
|----------|------|
| Filter a knowledge source at query time | https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-retrieve |
| Security filters (trimming) | https://learn.microsoft.com/azure/search/search-security-trimming-for-azure-search |
| Connect agents to Foundry IQ (per-user auth limits) | https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect |
| Document-level access control | https://learn.microsoft.com/azure/search/search-document-level-access-overview |
