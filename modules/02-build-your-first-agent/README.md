# Module 2: Build Your First Agent (Low-Code) (60 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 1 complete (Foundry resource + GPT-5.4 deployed)

---

## Objective

Create the **Contoso Estimator Advisor** agent using the Foundry portal with File Search (rate library + estimation policy) and Code Interpreter (cost calculations).

---

## Topics

### 2.1 Agent Types in Microsoft Foundry

| Type | Description | Best For |
|------|-------------|----------|
| **Prompt Agent** | Portal-configured agent with system instructions + tools. No custom code. | Rapid prototyping, business-user agents, workshop demos |
| **Hosted Agent** | Container-based agents running custom code (Python/C#) with full framework control. | Complex orchestration, multi-agent workflows, production pipelines |

This module uses **Prompt Agent** — the low-code path.

### 2.2 Scenario: Contoso Estimator Advisor

**Contoso Infrastructure** is a large-scale construction and engineering company. Their estimators prepare project bids by:
- Looking up **rate libraries** (labor, plant, materials by region/trade)
- Referencing **company policies** (margin guidelines, approval thresholds)
- Performing **cost calculations** (preliminary estimates from BOQ quantities)
- Searching **project history** for comparable data (added in Module 3)

The agent we build today handles the first three — simple document retrieval + calculations.

### 2.3 The Agent Loop

```
User Query → Agent (LLM) → Reason → Select Tool(s) → Execute → Synthesise → Response
                  ↑                                        │
                  └────────────── iterate if needed ───────┘
```

| Concept | Description |
|---------|-------------|
| **System Prompt** | Instructions defining persona, knowledge scope, response rules |
| **Tools** | Capabilities the agent can invoke — document search, calculations |
| **Grounding** | Connecting the agent to real data so it doesn't hallucinate |
| **Orchestration** | Agent decides which tool(s) to call, in what order |

### 2.4 Tools We'll Use

| Tool | Type | Purpose in This Module |
|------|------|----------------------|
| **File Search** | Built-in | Search rate library + estimation policy documents |
| **Code Interpreter** | Built-in | Calculate costs from BOQ quantities and rates |
| **Memory** (preview) | Built-in | Remember estimator preferences and prior queries across sessions |

---

## Demo: Create the Contoso Estimator Advisor

### Pre-Demo Setup Checklist

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Module 1 complete | Foundry resource + project exist | Project visible at ai.azure.com |
| 2 | GPT-5.4 deployed | Model deployment active | Shows in Build > Models |
| 3 | Embedding model deployed | Deploy `text-embedding-3-small` in project | Shows in Build > Models |
| 4 | Sample data prepared | Rate library + policy PDFs | Files ready to upload |

### Demo Steps

**Step 1: Create the Agent**

1. In Foundry portal → select project `contoso-estimator`
2. Navigate to **Build** → **Agents**
3. Click **Create agent**
4. Configure:
   - Agent name: `contoso-estimator-advisor`
   - Model: `gpt-5-4` (deployed in Module 1)

**Step 2: Write System Instructions**

Paste the following system prompt:

```
You are the Contoso Estimator Advisor, an AI assistant for Contoso Infrastructure's
estimation and tendering teams.

## Your Role
- Help estimators look up rates from the company rate library
- Provide guidance on estimation policies and approval thresholds
- Perform cost calculations when given quantities and rates
- Compare rates across different regions and trades

## Data Source Policy
- For rates, policies, and project data: ONLY use information from your connected
  tools (File Search, Code Interpreter). Never use training knowledge for specific numbers.
- Always cite your source: "According to [document name]..." or "Source: [file name]"
- If you cannot find the answer in connected documents, respond:
  "I don't have that information in my current documents. You may need to check
  [suggested source]."

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
```

**Step 3: Add File Search Tool**

1. In the agent configuration, click **+ Add tool**
2. Select **File Search**
3. Upload the sample documents:
   - `contoso-rate-library.pdf` — unit rates by trade and region
   - `contoso-estimation-policy.pdf` — margin guidelines and approval matrix
4. Wait for indexing to complete (green status)

**Step 4: Add Code Interpreter Tool**

1. Click **+ Add tool** again
2. Select **Code Interpreter**
3. No configuration needed — it's ready to use

**Step 5: Add Memory Tool (Preview)**

> ⚠️ Preview feature — may change

Memory enables the agent to remember user preferences and context across sessions. It works in three phases: **extraction** (captures key facts from conversations), **consolidation** (merges and deduplicates), and **retrieval** (surfaces relevant memories in future sessions).

Three memory types are extracted automatically:

| Memory Type | Description | Example |
|-------------|-------------|---------|
| **User profile** | Durable preferences and personal context | "I mostly work on NSW projects" |
| **Chat summary** | Distilled summaries of prior conversations | "Previously calculated formwork costs for QLD" |
| **Procedural** | Reusable how-to patterns from prior interactions | "User always requests breakdown by trade then total" |

**Create the memory store:**

1. In Foundry portal → select project `contoso-estimator`
2. Navigate to **Build** → **Memory stores**
3. Click **Create memory store**
4. Configure:
   - Name: `contoso-estimator-memory`
   - Chat model: `gpt-5-4` (deployed in Module 1)
   - Embedding model: `text-embedding-3-small` (deployed in prerequisites)
   - Enable: ✅ User profile, ✅ Chat summary, ✅ Procedural memory
   - User profile details: `Store estimator preferences: preferred regions, commonly used trades, default BOQ templates, and rate lookup patterns. Avoid storing sensitive financial data like margins or markups.`
5. Click **Create** and wait for the store to be ready

**Attach memory to the agent:**

1. Return to the agent configuration for `contoso-estimator-advisor`
2. Click **+ Add tool**
3. Select **Memory Search (Preview)**
4. Configure:
   - Memory store: `contoso-estimator-memory`
   - Scope: `{{$userId}}` (isolates memories per user)
   - Update delay: `300` seconds (5 minutes — default)
5. Click **Save**

> **Pro-code equivalent:** For the SDK version of memory store creation and configuration, see **Module 11 — Step 02b** (`.NET`) which shows the `MemoryStoreDefaultDefinition` and `MemorySearchPreviewTool` APIs.

**Step 6: Save and Test**

1. Click **Save**
2. In the playground, test with these queries:

**Query 1 — Rate Lookup:**
```
What is the current concrete supply and pour rate for NSW?
```

**Query 2 — Policy Question:**
```
What is the approval threshold for tenders over $50M?
```

**Query 3 — Cost Calculation:**
```
Calculate the cost for the following BOQ items:
- 2,500 m³ concrete at the NSW rate
- 800 tonnes structural steel at the VIC rate
- 15,000 m² formwork at the QLD rate

Show the breakdown and total.
```

**Query 4 — Memory (Session 1 — Establish Preferences):**
```
I mostly work on NSW projects and usually need concrete and formwork rates.
Remember that for future queries.
```
*(Agent should acknowledge and store the preference)*

**Query 5 — Memory (Session 2 — Recall in New Conversation):**

Start a **new conversation** in the playground, then ask:
```
What are the current rates for my usual trades?
```
*(Agent should recall NSW, concrete, and formwork from memory and return the relevant rates)*

> **Presenter note:** There is an `update_delay` (default 5 min) before memories are persisted. For the demo, set this to a low value (e.g., 1 second) or wait briefly between sessions. The memory extraction happens after a period of inactivity.

**Query 6 — Boundary Test:**
```
What margin does Contoso typically apply to government projects?
```
*(Should decline per system prompt boundaries)*

---

## 2.5 System Prompt Engineering Best Practices

| Principle | Description | Example |
|-----------|-------------|---------|
| **Role definition** | Clear persona in first sentence | "You are the Contoso Estimator Advisor..." |
| **Data policy** | Explicit rules about what sources to use | "ONLY use information from connected tools" |
| **Citation requirement** | Force attribution | "Always cite your source" |
| **Boundaries** | What the agent should NOT do | "Do NOT disclose margin percentages" |
| **Format guidance** | How to structure responses | "Use tables for rate comparisons" |
| **Fallback behavior** | What to say when it can't answer | "I don't have that information..." |

### Common Mistakes to Avoid

| Mistake | Why It's Bad | Fix |
|---------|-------------|-----|
| No data policy | Agent uses training knowledge for specific numbers | Add explicit "ONLY use connected tools" rule |
| No boundaries | Agent answers anything, including sensitive topics | Define what it should NOT do |
| Vague role | Responses are generic and unhelpful | Be specific about domain and expertise |
| No citation rule | Can't verify where answers came from | Require source attribution |

---

## 2.6 What the Agent Can Do Now

After this module, the Contoso Estimator Advisor can:

| Capability | Tool Used | Example |
|-----------|-----------|---------|
| Look up rates by trade/region | File Search | "What's the earthworks rate in QLD?" |
| Explain estimation policies | File Search | "What's the approval matrix for large tenders?" |
| Calculate costs from BOQ | Code Interpreter | "Calculate total for 500m³ concrete at $X/m³" |
| Combine lookup + calculation | File Search + Code Interpreter | "What would 1000m² formwork cost in NSW?" |
| Remember user preferences | Memory | "I usually work on NSW projects" (recalled next session) |
| Recall prior context | Memory + File Search | "What are my usual rates?" (uses stored preferences) |

**What it CAN'T do yet** (added in later modules):
- Search across historical project data → Module 3 (Foundry IQ)
- Get live market pricing → Module 5 (Web Search / OpenAPI)
- Refuse prompt injection attacks → Module 4 (Guardrails)

---

## Key Takeaways

1. **Prompt Agent** = LLM + system instructions + tools — no code required
2. The **system prompt** is the most important quality lever — invest time here
3. **File Search** handles document RAG automatically (chunking, embedding, retrieval)
4. **Code Interpreter** gives the agent the ability to calculate and analyze data
5. A well-designed agent clearly states what it can and cannot answer

---

## What's Next

In **Module 3**, we connect the agent to **Foundry IQ** — a managed knowledge base backed by Azure AI Search — to search across historical project data stored in Azure Blob Storage.

---

## References

| Resource | Link |
|----------|------|
| Agent Service Overview | https://learn.microsoft.com/azure/foundry/agents/concepts/workflow |
| Tool Catalog | https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog |
| File Search | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/file-search |
| Code Interpreter | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/code-interpreter |
| Memory Concepts | https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-memory |
| Memory How-To | https://learn.microsoft.com/azure/foundry/agents/how-to/memory-usage |
| System Prompt Best Practices | https://learn.microsoft.com/azure/foundry/agents/how-to/create-agent#system-instructions |
