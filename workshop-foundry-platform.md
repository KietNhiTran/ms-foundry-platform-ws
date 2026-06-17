# Microsoft Foundry AI Platform Workshop

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo (Presenter demonstrates, audience observes)  
**Fictional Company:** Contoso Corporation  

---

## Workshop Overview

A comprehensive enablement workshop covering the **Microsoft Foundry** platform — the unified Azure platform-as-a-service for enterprise AI operations, model building, and agent development. This workshop uses **Contoso Corporation** as a fictional enterprise scenario to demonstrate real-world patterns.

> **Microsoft Foundry** unifies agents, models, and tools under a single management grouping with built-in enterprise-readiness: tracing, monitoring, evaluations, and customizable enterprise setup configurations.
>
> **Reference:** [What is Microsoft Foundry?](https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio)

---

## Workshop Modules (Pick Your Path)

This workshop is **modular** — select modules based on audience interest and time available.

| Module | Duration | Topic | Recommended For |
|--------|----------|-------|-----------------|
| **1** | 45 min | Foundry Platform Overview & Setup | All audiences |
| **2** | 60 min | Build Your First Agent (Low-Code) | All audiences |
| **3** | 45 min | Agentic RAG & Foundry IQ | RAG / Knowledge retrieval focus |
| **4** | 45 min | Content Safety & Guardrails | Security / Compliance focus |
| **5** | 45 min | Foundry Toolkit & Data Connectors | Data integration focus |
| **6** | 45 min | Observability & Tracing | Operations / SRE focus |
| **7** | 45 min | Evaluation & Continuous Monitoring | Quality / MLOps focus |
| **8** | 40 min | AI Gateway & Token Governance | Cost / Governance focus |
| **9** | 50 min | Safety & Red Teaming | Security / Red team focus |
| **10** | 50 min | Fleet Management & Agent 365 | Enterprise scale / M365 focus |
| **11** | 45 min | Pro-Code Development (.NET / Python) | Developer audience |

---

## Suggested Workshop Configurations

### Half-Day Essentials (~3.5 hours)

For teams new to Foundry — covers the core platform capabilities.

| Time | Module | Topic |
|------|--------|-------|
| 0:00 – 0:45 | **Module 1** | Foundry Platform Overview & Setup |
| 0:45 – 1:45 | **Module 2** | Build Your First Agent (Low-Code) |
| 1:45 – 2:00 | *Break* | |
| 2:00 – 2:45 | **Module 3** | Agentic RAG & Foundry IQ |
| 2:45 – 3:30 | **Module 4** | Content Safety & Guardrails |

### Full-Day Comprehensive (~7 hours)

For teams requiring deep platform coverage including governance and operations.

| Time | Module | Topic |
|------|--------|-------|
| 9:00 – 9:45 | **Module 1** | Foundry Platform Overview & Setup |
| 9:45 – 10:45 | **Module 2** | Build Your First Agent (Low-Code) |
| 10:45 – 11:00 | *Break* | |
| 11:00 – 11:45 | **Module 3** | Agentic RAG & Foundry IQ |
| 11:45 – 12:30 | **Module 4** | Content Safety & Guardrails |
| 12:30 – 13:15 | *Lunch* | |
| 13:15 – 14:00 | **Module 6** | Observability & Tracing |
| 14:00 – 14:45 | **Module 7** | Evaluation & Continuous Monitoring |
| 14:45 – 15:00 | *Break* | |
| 15:00 – 15:40 | **Module 8** | AI Gateway & Token Governance |
| 15:40 – 16:30 | **Module 10** | Fleet Management & Agent 365 |
| 16:30 – 17:00 | Wrap-up | Q&A, Next Steps |

### Developer Deep-Dive (~4 hours)

For development teams focusing on pro-code patterns.

| Time | Module | Topic |
|------|--------|-------|
| 0:00 – 0:45 | **Module 1** | Foundry Platform Overview & Setup |
| 0:45 – 1:30 | **Module 2** | Build Your First Agent (Low-Code) |
| 1:30 – 1:45 | *Break* | |
| 1:45 – 2:30 | **Module 11** | Pro-Code Development (.NET / Python) |
| 2:30 – 3:15 | **Module 3** | Agentic RAG & Foundry IQ |
| 3:15 – 4:00 | **Module 7** | Evaluation & Continuous Monitoring |

### Security & Governance Focus (~4 hours)

For security, compliance, and platform governance teams.

| Time | Module | Topic |
|------|--------|-------|
| 0:00 – 0:45 | **Module 1** | Foundry Platform Overview & Setup |
| 0:45 – 1:30 | **Module 4** | Content Safety & Guardrails |
| 1:30 – 1:45 | *Break* | |
| 1:45 – 2:35 | **Module 9** | Safety & Red Teaming |
| 2:35 – 3:15 | **Module 8** | AI Gateway & Token Governance |
| 3:15 – 4:00 | **Module 10** | Fleet Management & Agent 365 |

---

## Module Descriptions

### Module 1: Foundry Platform Overview & Setup (45 min)

**Objective:** Understand Microsoft Foundry's architecture, provision resources, and deploy a foundation model.

**Topics:**
- What is Microsoft Foundry — unified platform for agents, models, and tools
- Evolution from Azure AI Studio → Azure AI Foundry → Microsoft Foundry
- V2 architecture: Foundry resource → projects (flat resource model)
- Model Catalog — 1,900+ models (GPT-5, GPT-4.1, Claude, Mistral, Llama, etc.)
- Resource provisioning and project creation
- Model deployment (GPT-4.1 recommended for production workloads)
- RBAC roles: Azure AI User, AI Account Owner, AI Project Manager
- Entra ID authentication and managed identities

**Demo:** Create a Foundry resource, deploy GPT-4.1, run first API call

**Reference:** [Foundry Overview](https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio) | [Create Foundry Resources](https://learn.microsoft.com/azure/foundry/tutorials/quickstart-create-foundry-resources)

---

### Module 2: Build Your First Agent (Low-Code) (60 min)

**Objective:** Create a Prompt Agent using the Foundry portal with tools and memory.

**Scenario:** Build a "Contoso Operations Advisor" agent that answers questions about company policies, performs calculations, and retrieves live data.

**Topics:**
- Prompt Agent vs Hosted Agent — when to use each
- Agent configuration: model selection, system instructions, tools
- System prompt engineering best practices
- File Search tool — upload documents for RAG
- Code Interpreter tool — calculations and data analysis
- Memory — retain context across interactions
- Testing and prompt boundary validation

**Demo:** Create `contoso-operations-advisor` agent with File Search + Code Interpreter

**Reference:** [Agent Service Overview](https://learn.microsoft.com/azure/foundry/agents/concepts/workflow) | [Tool Catalog](https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog)

---

### Module 3: Agentic RAG & Foundry IQ (45 min)

**Objective:** Implement enterprise-grade retrieval using Azure AI Search and Foundry IQ knowledge bases.

**Topics:**
- RAG fundamentals: chunking, embedding, retrieval
- **Agentic RAG** — how it goes beyond single-shot RAG:
  - LLM-powered query planning & decomposition
  - Parallel subquery execution
  - Semantic reranking of combined results
  - Iterative retrieval (~36% higher response quality)
- Azure AI Search: index schema, semantic/vector/hybrid search
- **Foundry IQ** knowledge bases:
  - Multi-source knowledge integration (Blob, SharePoint, OneLake, Web)
  - Automated indexing and citation
  - Permission enforcement (ACLs, Purview sensitivity labels)
- Comparison: File Search vs Azure AI Search vs Foundry IQ

**Demo:** Create AI Search index, connect to agent, test cross-document retrieval

**Reference:** [What is Foundry IQ?](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq) | [Connect Knowledge Base](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect)

---

### Module 4: Content Safety & Guardrails (45 min)

**Objective:** Configure content filters, Prompt Shields, and guardrails for production agents.

**Topics:**
- Why content safety — protect against harmful content, jailbreaks, data leakage
- Default safety policies: Hate, Violence, Sexual, Self-Harm (Medium severity)
- Prompt Shields: jailbreak detection + document attack protection
- Guardrails architecture — 4 intervention points:
  - User input → Tool call → Tool response → Output
- Creating custom content filters
- Protected materials detection
- Responsible AI framework: Discover → Protect → Govern

**Demo:** Create guardrail, assign to agent, test with adversarial prompts

**Reference:** [Content Safety Overview](https://learn.microsoft.com/azure/ai-services/content-safety/overview) | [Guardrails](https://learn.microsoft.com/azure/foundry/guardrails/guardrails-overview)

---

### Module 5: Foundry Toolkit & Data Connectors (45 min)

**Objective:** Connect agents to enterprise data sources and external tools.

**Topics:**
- Tool ecosystem overview (1,400+ tools available)
- Built-in tools: File Search, Code Interpreter, Web Search, Image Generation
- **MCP (Model Context Protocol)** — connect to any MCP-compatible server
- **Azure AI Search** — enterprise search integration
- **Microsoft Fabric / Fabric IQ** — structured data queries
- **Work IQ** — Microsoft 365 content
- **SharePoint** — document grounding
- **Azure Functions** — serverless actions
- **OpenAPI** — custom REST APIs
- **A2A (Agent-to-Agent)** — multi-agent collaboration

**Demo:** Connect multiple data sources, show tool orchestration

**Reference:** [Tool Catalog](https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog) | [Fabric IQ](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/fabric-iq)

---

### Module 6: Observability & Tracing (45 min)

**Objective:** Connect Application Insights, explore tracing, and use the Agent Monitoring Dashboard.

**Topics:**
- Server-side tracing — automatic with zero code changes
- What a trace shows: spans, latency, token usage, tool calls
- Client-side instrumentation with `azure-monitor-opentelemetry`
- Custom attributes and correlation IDs
- Agent Monitoring Dashboard — fleet-level visibility
- KQL queries for trace analysis
- Alerting on anomalies

**Demo:** Connect App Insights, view traces, explore monitoring dashboard

**Reference:** [Trace Your Application](https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-setup) | [Monitor Agents Dashboard](https://learn.microsoft.com/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard)

---

### Module 7: Evaluation & Continuous Monitoring (45 min)

**Objective:** Run batch evaluations, set up continuous monitoring, and integrate into CI/CD.

**Topics:**
- Offline batch evaluation — test agents against labeled datasets
- Built-in evaluators: task adherence, intent resolution, tool call accuracy, groundedness
- Custom evaluators for domain-specific criteria
- **Continuous evaluation** — auto-evaluate live production traffic
- Evaluation rules and sampling
- LLM-as-judge patterns
- CI/CD integration with GitHub Actions
- Evaluation-driven deployment gates

**Demo:** Create evaluation dataset, run batch eval, set up continuous monitoring

**Reference:** [Evaluate Agentic Workflows](https://learn.microsoft.com/azure/foundry/observability/how-to/evaluate-agent) | [Continuous Evaluation](https://learn.microsoft.com/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard#set-up-continuous-evaluation)

---

### Module 8: AI Gateway & Token Governance (40 min)

**Objective:** Enable AI Gateway for rate limiting, cost control, and governance.

**Topics:**
- AI Gateway architecture — APIM between clients and models
- Token rate limiting (TPM) and quotas
- Cost control and chargeback
- Semantic caching (reduce redundant calls)
- Content safety policies at gateway level
- MCP tool governance
- Usage analytics and reporting

**Demo:** Associate APIM, configure TPM limits, trigger rate limiting

**Reference:** [AI Gateway](https://learn.microsoft.com/azure/foundry/control-plane/how-to/ai-gateway) | [Plan and Manage Costs](https://learn.microsoft.com/azure/foundry/concepts/manage-costs)

---

### Module 9: Safety & Red Teaming (50 min)

**Objective:** Run automated safety scans and proactive vulnerability testing.

**Topics:**
- AI Red Teaming Agent — automated adversarial testing
- Risk categories: jailbreak, harmful content, data leakage, bias
- Attack strategies and simulation
- Red teaming via portal UI and SDK
- Compliance policies across subscription
- Microsoft Defender for Cloud integration
- Security posture assessment
- Remediation workflows

**Demo:** Run red teaming scan, review scorecard, remediate findings

**Reference:** [AI Red Teaming Agent](https://learn.microsoft.com/azure/foundry/concepts/ai-red-teaming-agent) | [Defender for AI](https://learn.microsoft.com/azure/defender-for-cloud/alerts-ai-workloads)

---

### Module 10: Fleet Management & Agent 365 (50 min)

**Objective:** Manage agents at enterprise scale and publish to Microsoft 365.

**Topics:**
- **Foundry Control Plane** — single pane of glass for all AI assets
- Fleet-wide KPIs: active agents, run completion, cost trends, prevented behaviors
- Agent inventory and lifecycle management
- Registering external/non-Foundry agents
- Health scoring and alerting
- **Publishing to Microsoft 365**:
  - Teams integration
  - BizChat deployment
  - Azure Bot Service
- Agent 365 enterprise governance
- Multi-tenant considerations

**Demo:** Explore fleet dashboard, publish agent to Teams

**Reference:** [Foundry Control Plane](https://learn.microsoft.com/azure/foundry/control-plane/overview) | [Publish to Copilot](https://learn.microsoft.com/azure/foundry/agents/how-to/publish-copilot)

---

### Module 11: Pro-Code Development (.NET / Python) (45 min)

**Objective:** Build agents programmatically using the Foundry SDK.

**Topics:**
- SDK overview: `azure-ai-projects` (Python) / `Azure.AI.Projects` (C#)
- Authentication: DefaultAzureCredential, managed identities
- Programmatic agent creation with tools
- Wiring Azure AI Search, File Search, Code Interpreter in code
- Responses API vs legacy Assistants API
- Building a chat UI with SSE streaming
  - Python: FastAPI + sse-starlette
  - C#: ASP.NET Core minimal API
- Evaluation SDK for CI/CD integration
- Best practices for production deployments

**Demo:** Create agent in code, build streaming chat UI

**Reference:** [SDK Overview](https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview) | [Quickstart Code](https://learn.microsoft.com/azure/foundry/quickstarts/get-started-code)

---

## What Attendees Will Learn

After this workshop, attendees will understand:

1. **Platform Architecture** — Foundry resource model, projects, V2 architecture
2. **Agent Development** — Low-code and pro-code patterns for building agents
3. **Knowledge Integration** — RAG patterns with Azure AI Search and Foundry IQ
4. **Content Safety** — Guardrails, content filters, and Responsible AI controls
5. **Data Connectivity** — Tool ecosystem and enterprise data integration
6. **Observability** — Tracing, monitoring, and operational visibility
7. **Quality Assurance** — Evaluation, continuous monitoring, and CI/CD integration
8. **Governance** — AI Gateway, token management, and cost control
9. **Security** — Red teaming, threat detection, and compliance
10. **Enterprise Scale** — Fleet management and Microsoft 365 publishing

---

## Prerequisites

**For presenters:**
- Azure subscription with Foundry resource access
- Foundry User + Account Owner roles
- Pre-provisioned resources per module checklists

**For attendees (led demo format):**
- No setup required — this is a presenter-led demonstration
- Bring questions specific to your organization's use cases

---

## Reference Materials

| Resource | Link |
|----------|------|
| Microsoft Foundry Portal | https://ai.azure.com |
| Foundry Documentation | https://learn.microsoft.com/azure/foundry/ |
| SDK Reference (Python) | https://learn.microsoft.com/python/api/overview/azure/ai-projects-readme |
| SDK Reference (.NET) | https://learn.microsoft.com/dotnet/api/overview/azure/ai.projects-readme |
| Agent Service Overview | https://learn.microsoft.com/azure/foundry/agents/concepts/workflow |
| Tool Catalog | https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog |
| Foundry IQ | https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq |
| Content Safety | https://learn.microsoft.com/azure/ai-services/content-safety/overview |
| Control Plane | https://learn.microsoft.com/azure/foundry/control-plane/overview |

---

## Workshop Customization

This workshop is designed to be customized for specific audiences:

1. **Replace "Contoso"** with customer-specific scenario (if desired)
2. **Select modules** based on audience role and interest
3. **Adjust timing** — modules can be compressed or expanded
4. **Add domain context** — customize system prompts and sample data for the audience's industry

For hands-on versions, convert "Led Demo" to "Hands-On" and ensure attendees have Azure subscription access and local development environments configured.
