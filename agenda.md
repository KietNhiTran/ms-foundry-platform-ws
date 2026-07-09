# Microsoft Foundry AI Platform Workshop — Agenda

**Format:** Full-Day Led Demo (presenter-driven, audience observes)
**Duration:** ~7 hours
**Scenario:** Contoso Estimator Advisor — an AI agent for a fictional construction & engineering company that helps estimators prepare project bids.

---

## Overview

A comprehensive enablement workshop covering the **Microsoft Foundry** platform — the unified Azure platform for enterprise AI operations, model building, and agent development. We progressively build a single production-grade agent across the day, adding capabilities module by module.

---
### Scenario: Contoso Estimation Advisor

Throughout this workshop, we progressively build a single agent — the **Contoso Estimator Advisor** — for **Contoso Infrastructure**, a fictional large-scale construction and engineering company. The agent helps estimators prepare project bids by:

- Searching **historical project data** (past bids, final costs, lessons learned)
- Looking up **rate libraries** (labor, plant, materials by region)
- Referencing **company policies** (margin guidelines, approval thresholds)
- Performing **cost calculations** (preliminary estimates from BOQ quantities)

## Agenda

| Time | Module | Topic | What You'll See |
|------|--------|-------|-----------------|
| 09:00 – 09:45 | **Module 1** | Foundry Platform Overview & Setup | Architecture, resource provisioning, GPT 5.4 deployment, RBAC |
| 09:45 – 10:45 | **Module 2** | Build Your First Agent (Low-Code) | Create the Contoso Estimator agent with File Search + Code Interpreter |
| 10:45 – 11:00 | *Break* | | |
| 11:00 – 11:45 | **Module 3** | Agentic RAG & Foundry IQ | Multi-source knowledge bases, semantic retrieval, citation |
| 11:45 – 12:30 | **Module 4** | Content Safety & Guardrails | Prompt Shields, custom filters, block confidential data leakage |
| 12:30 – 13:15 | *Lunch* | | |
| 13:15 – 14:00 | **Module 6** | Observability & Tracing | Application Insights, traces, Agent Monitoring Dashboard |
| 14:00 – 14:45 | **Module 11** | Pro-Code Development (.NET) | .NET SDK walkthrough — programmatic equivalent of the portal steps |
| 14:45 – 15:00 | *Break* | | |
| 15:00 – 15:40 | **Module 7** | Evaluation & Continuous Monitoring | Batch eval, LLM-as-judge, CI/CD integration |
| 15:40 – 16:15 | **Module 8** | AI Gateway & Token Governance | APIM integration, TPM limits, cost control, semantic caching |
| 16:15 – 16:30 | **Wrap-up** | Q&A and Next Steps | Discussion, customization options, follow-up resources |

---

## Key Outcomes

By the end of the day, attendees will understand how to:

1. Provision and configure a Microsoft Foundry resource
2. Build, ground, and secure production-grade agents
3. Integrate enterprise knowledge with Foundry IQ
4. Apply content safety and Responsible AI guardrails
5. Pro-code version of building the agent
6. Monitor agents with tracing and continuous evaluation
7. Govern AI usage with AI Gateway (rate limits, cost control)

---

## Format & Prerequisites

- **Led Demo** — presenter demonstrates each module live; no setup required for attendees
- Bring questions specific to your organization's AI use cases
- Optional pre-reading: [What is Microsoft Foundry?](https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio)

---

## Optional Add-On Modules

Available on request to tailor the agenda:

| Module | Duration | Topic |
|--------|----------|-------|
| Module 5 | 45 min | Foundry Toolkit & Data Connectors (MCP, Fabric, SharePoint, OpenAPI) |
| Module 9 | 50 min | Safety & Red Teaming (automated adversarial testing) |
| Module 10 | 50 min | Fleet Management & Agent 365 (Control Plane, publish to Microsoft Teams) |

### Security Track (Enterprise Platform Security)

Fills the enterprise platform-security gaps (identity, network, data) beyond the runtime security already in Modules 4 and 9. Deliver together (~90 min) or individually.

| Module | Duration | Topic |
|--------|----------|-------|
| Module 12 | 45 min | Secure Agent Identity & Access (Agent Identity / Entra Agent ID, OBO passthrough, least-privilege RBAC) |
| Module 13 | 45 min | Network Isolation & Data Protection (private endpoints, VNet injection, CMK, data residency, BYO storage) |

> Governance topics not shown live — Defender for Cloud, Azure Policy, Purview DLP, security baseline, compliance — are collected in [shared/docs/security-governance.md](shared/docs/security-governance.md).

---

