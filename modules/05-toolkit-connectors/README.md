# Module 5: Foundry Toolkit & Data Connectors (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 2 complete (Contoso Estimator agent exists)

---

## Objective

Demonstrate the breadth of Foundry's tool ecosystem by adding Code Interpreter for cost calculations and discussing how external tools (OpenAPI, MCP, Azure Functions) extend agent capabilities.

---

## Topics

### 5.1 Tool Ecosystem Overview

Microsoft Foundry provides **1,400+ tools** across several categories:

| Category | Tools | Use Case |
|----------|-------|----------|
| **Built-in** | File Search, Code Interpreter, Web Search, Image Generation | Core agent capabilities |
| **MCP Servers** | Any MCP-compatible server | External data sources, APIs |
| **Azure AI Search** | Vector/hybrid/semantic search | Enterprise search integration |
| **Foundry IQ** | Managed knowledge bases | Multi-source RAG (Module 3) |
| **Fabric IQ** | Structured data queries | Business data from Fabric/Power BI |
| **Work IQ** | M365 content | Documents, emails, meetings |
| **OpenAPI** | Custom REST APIs | Any HTTP endpoint |
| **Azure Functions** | Serverless actions | Custom business logic |
| **A2A** | Agent-to-Agent protocol | Multi-agent collaboration |

### 5.2 Tools for the Contoso Estimator

| Tool | Already Added | Purpose |
|------|:---:|---------|
| File Search | ✅ Module 2 | Rate library + policy docs |
| Code Interpreter | ✅ Module 2 | Cost calculations |
| Foundry IQ | ✅ Module 3 | Project history retrieval |
| Web Search | 🔲 This module | Live material pricing |
| OpenAPI (mock) | 🔲 This module | External pricing API |

### 5.3 How Tool Orchestration Works

The agent's LLM decides **which tools to call, in what order**:

```
User: "Calculate total earthworks cost for 50,000m³ in QLD, 
       and check if current market prices have changed."

Agent reasoning:
  1. File Search → look up QLD earthworks rate from rate library
  2. Web Search → check current market price for comparison
  3. Code Interpreter → calculate: 50,000 × rate = total
  4. Synthesize → present calculation + market comparison
```

---

## Demo: Add Web Search + Discuss OpenAPI Pattern

### Demo Steps

**Step 1: Add Web Search Tool**

1. In agent configuration → click **+ Add tool**
2. Select **Web Search** (Bing Grounding)
3. Save — no additional configuration needed

**Step 2: Test Multi-Tool Orchestration**

```
Query: "I need to estimate 50,000 m³ of bulk earthworks in QLD. 
Use our rate library for the base rate, and check if current 
steel prices have increased in the last 3 months."
```

Observe: agent calls File Search (rates) + Web Search (market intel) + Code Interpreter (calculation)

**Step 3: Discuss OpenAPI Tool Pattern (Slide/Talking Point)**

For production, you'd connect to real systems via OpenAPI:

```yaml
# Example: contoso-pricing-api.yaml
openapi: 3.0.0
info:
  title: Contoso Material Pricing API
  version: 1.0.0
paths:
  /rates/{material}/{region}:
    get:
      summary: Get current material rate
      parameters:
        - name: material
          in: path
          required: true
          schema:
            type: string
            enum: [concrete, steel, earthworks, asphalt]
        - name: region
          in: path
          required: true
          schema:
            type: string
            enum: [NSW, VIC, QLD, WA]
      responses:
        '200':
          description: Current rate
          content:
            application/json:
              schema:
                type: object
                properties:
                  material:
                    type: string
                  region:
                    type: string
                  rate:
                    type: number
                  unit:
                    type: string
                  last_updated:
                    type: string
                    format: date
```

**Step 4: Discuss MCP Pattern (Talking Point)**

MCP (Model Context Protocol) enables connecting to external data sources:
- Databricks → query structured project data via natural language
- Custom MCP servers → expose any internal system as a tool
- The agent calls MCP tools just like built-in tools

---

## Key Takeaways

1. Foundry's tool ecosystem spans **1,400+ tools** across multiple categories
2. The agent **autonomously orchestrates** tool calls — no hard-coded workflow
3. **Web Search** adds real-time grounding with zero setup
4. **OpenAPI** connects to any REST API (production pattern for internal systems)
5. **MCP** is the open standard for connecting AI agents to external tools and data

---

## References

| Resource | Link |
|----------|------|
| Tool Catalog | https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog |
| Code Interpreter | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/code-interpreter |
| Web Search | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/bing-grounding |
| OpenAPI Tools | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/openapi |
| MCP | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/model-context-protocol |
| Fabric IQ | https://learn.microsoft.com/azure/foundry/agents/how-to/tools/fabric-iq |
