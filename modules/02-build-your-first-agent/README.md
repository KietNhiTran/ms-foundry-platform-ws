# Module 2: Build Your First Agent (Low-Code)

**Duration:** 60 minutes
**Format:** Led Demo (Presenter demonstrates, audience observes)

## Objective

Create a Prompt Agent using the Foundry portal with tools and memory to build the Contoso Estimator Advisor.

## Topics

- Prompt Agent vs Hosted Agent — when to use each
- Agent configuration: model selection, system instructions, tools
- System prompt engineering best practices
- File Search tool — upload documents for RAG
- Code Interpreter tool — calculations and data analysis
- Memory — retain context across interactions
- Testing and prompt boundary validation

---

## Concept Overview (15 min)

### What Is a Foundry Agent?

An agent is an AI application that uses a model from the Foundry model catalog to reason about user requests and take autonomous actions to fulfill them. Unlike a simple chatbot that only generates text, an agent can call tools, access external data, and make decisions across multiple steps to complete a task.

Every agent combines three core components:

1. **Model** — A model from the Foundry model catalog that provides reasoning and language capabilities (e.g., GPT-4.1).
2. **Instructions** — Define goals, constraints, and behavior. In Foundry, instructions can be prompt-based (Prompt Agent) or code-based (Hosted Agent).
3. **Tools** — Provide access to data or actions, such as File Search, Code Interpreter, or API calls.

### Prompt Agent vs Hosted Agent

| Aspect | Prompt Agent | Hosted Agent (Preview) |
|--------|-------------|----------------------|
| **Definition** | Configured via instructions, model, and tools — no application code | Your agent code, run by Foundry |
| **Authoring** | Portal UI or SDK/REST API | Agent Framework, LangGraph, OpenAI Agents SDK, or custom code |
| **Runtime** | Fully managed by Foundry | Managed endpoint with auto-scaling |
| **Infrastructure** | No compute to manage | Container image or source zip |
| **Best for** | Getting started fast, internal tools, managed agents without custom orchestration | Custom orchestration logic, multi-agent systems, custom protocols |
| **When to use** | You want Foundry to handle everything — no servers, no containers | You need full control over agent logic while Foundry handles hosting and scaling |

> **For this workshop**, we use Prompt Agents exclusively — they let us focus on agent capabilities without infrastructure concerns.

### Responses API

The Responses API is the single entry point for every agent type in Foundry. It gives any framework, process, or runtime access to Foundry models plus platform tools (File Search, Code Interpreter, Memory, Web Search, MCP servers). Whether you create an agent in the portal or via code, the Responses API handles model inference and tool orchestration.

---

## Agent Configuration Deep-Dive (10 min)

### Model Selection

For the Contoso Estimator Advisor, we recommend **GPT-4.1** for production workloads:

- Strong reasoning for cost estimation calculations
- Reliable tool calling for File Search and Code Interpreter
- Good instruction following for system prompt constraints
- Supports all built-in tools (File Search, Code Interpreter, Web Search, etc.)

> **Tip:** You can swap models without changing your agent configuration. Start with GPT-4.1 and test alternatives from the catalog if needed.

### System Prompt Engineering

A well-crafted system prompt defines the agent's behavior, boundaries, and personality. For the Contoso Estimator Advisor:

**Best practices:**

1. **Define the role clearly** — "You are a cost estimation advisor for Contoso Infrastructure."
2. **Specify capabilities** — "You can look up rates, calculate costs, and reference company policies."
3. **Set boundaries** — "Only answer questions related to construction estimation. Do not provide legal or financial advice."
4. **Define output format** — "Present cost breakdowns in tables. Include units and rates."
5. **Handle unknowns** — "If you cannot find a rate in the library, say so explicitly rather than guessing."

**Example system prompt for the demo:**

```text
You are the Contoso Estimator Advisor, a cost estimation assistant for Contoso Infrastructure,
a large-scale construction and engineering company.

Your capabilities:
- Look up labor, plant, and materials rates from the Contoso Rate Library
- Reference company estimation policies including margin guidelines and approval thresholds
- Perform cost calculations from Bill of Quantities (BOQ) data
- Provide preliminary project estimates

Rules:
- Only answer questions related to construction cost estimation
- Always cite the source document when referencing rates or policies
- Present cost breakdowns in tabular format with units, quantities, and rates
- If a rate is not found in the library, state this explicitly — do not estimate
- Do not provide legal, financial, or contractual advice
- Round all currency values to two decimal places
```

---

## Tools Configuration (10 min)

### File Search

File Search augments agents with knowledge from uploaded files. Foundry automatically chunks, indexes, and stores documents in a vector store for retrieval. When the agent receives a query, it searches the vector store for relevant content and uses it to generate grounded responses.

**For this demo, we upload two documents:**

| Document | Purpose | Content |
|----------|---------|---------|
| `contoso-rate-library.pdf` | Rate lookup | Labor, plant, and materials rates organized by trade and region |
| `contoso-estimation-policy.pdf` | Policy reference | Margin guidelines, approval matrix, estimation procedures |

**How File Search works:**

1. Upload files to the agent's vector store
2. Foundry automatically chunks and indexes the content
3. When the agent receives a question, it searches the vector store
4. Relevant chunks are injected into the model's context
5. The model generates a grounded response with citations

### Code Interpreter

Code Interpreter allows the agent to write and run Python code in a sandboxed environment. This is essential for the Contoso Estimator because it enables:

- **Cost calculations** — Multiply quantities × rates from the rate library
- **Data analysis** — Summarize and compare rates across regions or trades
- **Formatted output** — Generate tables, charts, and structured reports

**Example interaction:**

> **User:** "Calculate the total labor cost for 500m² of concrete formwork using Sydney rates."
>
> **Agent:** *(Searches rate library for Sydney formwork rates, then uses Code Interpreter to calculate)*
> | Item | Quantity | Unit | Rate | Total |
> |------|----------|------|------|-------|
> | Formwork labor | 500 | m² | $45.00 | $22,500.00 |

### Memory (Preview)

Memory in Foundry Agent Service is a managed, long-term memory solution that enables agent continuity across sessions. It allows the agent to:

- **Retain user preferences** — Remember a user's preferred region or trade focus
- **Maintain conversation context** — Build on previous estimation conversations
- **Deliver personalized experiences** — Adapt responses based on past interactions

Memory extracts meaningful information from conversations, consolidates it into durable knowledge, and makes it available across sessions.

> ⚠️ **Preview feature** — Memory is currently in preview and may change.

---

## Demo: Create the Contoso Estimator Advisor (20 min)

### Pre-Demo Setup Checklist

Complete these steps before the live demo to ensure a smooth presentation.

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Foundry resource provisioned | Use the resource created in Module 1 | Resource appears in [foundry.microsoft.com](https://foundry.microsoft.com) |
| 2 | Project created | Use the project created in Module 1 | Project is accessible in the Foundry portal |
| 3 | GPT-4.1 model deployed | Use the deployment from Module 1 | Model appears in project's model deployments |
| 4 | Sample data files ready | Download `contoso-rate-library.pdf` and `contoso-estimation-policy.pdf` from `modules/02-build-your-first-agent/data/` | Both PDF files are saved locally |
| 5 | Browser logged in | Sign in to [foundry.microsoft.com](https://foundry.microsoft.com) with your Entra ID account | Portal loads with your project visible |
| 6 | Screen sharing ready | Set browser zoom to 125% for readability | Text is legible on projected screen |

### Demo Steps

#### Step 1: Navigate to Agent Builder

1. Open [foundry.microsoft.com](https://foundry.microsoft.com) and select your project
2. In the left navigation, select **Agents** under the Build section
3. Click **+ New agent** to open the agent builder

#### Step 2: Configure the Agent

1. **Name:** Enter `contoso-estimator-advisor`
2. **Model:** Select your deployed GPT-4.1 model
3. **Instructions:** Paste the system prompt from the [System Prompt Engineering](#system-prompt-engineering) section above

> **Presenter note:** Walk through each field, explaining why GPT-4.1 is chosen (strong reasoning, reliable tool use) and how the system prompt constrains the agent's behavior.

#### Step 3: Add File Search Tool

1. In the **Tools** section, click **+ Add tool**
2. Select **File Search**
3. Click **Upload files** and select `contoso-rate-library.pdf`
4. Upload `contoso-estimation-policy.pdf` as a second file
5. Wait for indexing to complete (a progress indicator shows the status)

> **Presenter note:** Explain that Foundry automatically chunks and indexes the documents. The agent will search this vector store when answering questions about rates or policies.

#### Step 4: Add Code Interpreter Tool

1. Click **+ Add tool** again
2. Select **Code Interpreter**
3. No additional configuration is needed — the tool is ready to use

> **Presenter note:** Explain that Code Interpreter gives the agent the ability to write and run Python code. For estimators, this means the agent can perform calculations rather than just retrieving text.

#### Step 5: Enable Memory

1. In the agent configuration, locate the **Memory** toggle
2. Enable Memory for the agent

> **Presenter note:** Explain that Memory allows the agent to remember context across conversations — for example, if an estimator frequently works on Sydney projects, the agent can remember this preference.

#### Step 6: Test the Agent

Run the following test prompts in the agent playground to demonstrate capabilities:

**Test 1 — Rate lookup (File Search):**
```
What is the labor rate for a structural steel fixer in the Sydney region?
```

**Test 2 — Policy reference (File Search):**
```
What is the approval threshold for preliminary estimates at Contoso?
```

**Test 3 — Cost calculation (Code Interpreter):**
```
Calculate the total cost for 200 tonnes of structural steel supply and installation
using Sydney rates from the rate library.
```

**Test 4 — Prompt boundary (should decline):**
```
What is the best investment strategy for Contoso's profits?
```
*Expected: The agent should decline, stating it only handles construction cost estimation.*

**Test 5 — Memory verification:**
```
I primarily work on projects in the Brisbane region.
```
*Then start a new conversation and ask:*
```
What region do I usually work in?
```
*Expected: The agent recalls the Brisbane preference from the previous conversation.*

---

## Key Takeaways

| Concept | What You Learned |
|---------|-----------------|
| **Prompt Agent** | Configure an agent entirely through the portal — no code, no infrastructure |
| **System Prompt** | Define role, capabilities, boundaries, and output format to control agent behavior |
| **File Search** | Upload documents to give the agent grounded knowledge via vector search |
| **Code Interpreter** | Enable the agent to perform calculations and data analysis with Python |
| **Memory** | Retain context across conversations for personalized experiences |
| **Testing** | Validate both expected capabilities and prompt boundaries |

---

## What's Next

In **Module 3: Agentic RAG & Foundry IQ**, we connect a Foundry IQ knowledge base with project history data (past bids, final costs, lessons learned) to enable cross-document retrieval across the Contoso project portfolio.

> **Pro-code equivalent:** See Module 11, Step 2 — `Step02_CreateAgent.cs` demonstrates the SDK equivalent of creating an agent with File Search and Code Interpreter.

---

## References

| Topic | Link |
|-------|------|
| Agent Service Overview | [learn.microsoft.com/azure/foundry/agents/concepts/workflow](https://learn.microsoft.com/azure/foundry/agents/concepts/workflow) |
| Tool Catalog | [learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog](https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog) |
| File Search Tool | [learn.microsoft.com/azure/foundry/agents/how-to/tools/file-search](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/file-search) |
| Code Interpreter Tool | [learn.microsoft.com/azure/foundry/agents/how-to/tools/code-interpreter](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/code-interpreter) |
| Memory in Agent Service | [learn.microsoft.com/azure/foundry/agents/how-to/memory-usage](https://learn.microsoft.com/azure/foundry/agents/how-to/memory-usage) |
| System Prompt Best Practices | [learn.microsoft.com/azure/foundry/agents/concepts/tool-best-practice](https://learn.microsoft.com/azure/foundry/agents/concepts/tool-best-practice) |
