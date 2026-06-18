# Module 1: Foundry Platform Overview & Setup (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Azure subscription with permissions to create Cognitive Services resources

---

## Objective

Understand Microsoft Foundry's architecture, provision a Foundry resource and project, and deploy a foundation model (GPT-4.1) ready for agent development.

---

## Topics

### 1.1 What is Microsoft Foundry?

Microsoft Foundry is the unified Azure platform for building, deploying, and managing AI applications. It brings together models, tools, data connectors, and governance under a single portal at [ai.azure.com](https://ai.azure.com).

| Capability | What It Does |
|-----------|-------------|
| **Model Catalog** | Access 1,900+ models — OpenAI, Meta, Mistral, Cohere, and more |
| **Agent Service** | Low-code and pro-code agent development with built-in tool orchestration |
| **Tool Ecosystem** | 1,400+ tools: File Search, Code Interpreter, Web Search, MCP, OpenAPI, A2A |
| **Evaluation** | Batch evaluation with LLM-as-judge + continuous monitoring |
| **Tracing & Observability** | End-to-end trace logging via Azure Monitor and Application Insights |
| **Responsible AI** | Content safety filters, prompt shields, guardrails, red teaming |
| **Control Plane** | Fleet management, AI Gateway, cost governance |

### 1.2 Platform Evolution

```
Azure AI Studio (2023) → Azure AI Foundry (2024) → Microsoft Foundry (2025-current)
```

> The platform, portal URL (ai.azure.com), and underlying services remain the same — only the branding and architecture evolved.

### 1.3 V2 Resource Model (Flat Hierarchy)

The previous "Hub + Project" model is deprecated. The new architecture uses a flat **Foundry resource → Project** hierarchy:

```
Azure Subscription
  └── Resource Group
        └── Foundry Resource (Account)
              ├── Model Deployments (shared across projects)
              ├── Connections (AI Search, Storage, etc.)
              ├── Network Configuration
              └── Security & RBAC (control plane)
              │
              ├── Project A (child resource)
              │     ├── Agents
              │     ├── Evaluations
              │     ├── Traces
              │     └── Files & Data
              │
              └── Project B (child resource)
                    └── ...
```

| Concept | Description |
|---------|-------------|
| **Foundry resource** | IT/admin scope — governance, networking, model deployments, cost |
| **Project** | Developer scope — agents, evaluations, files, tracing |
| **ARM resource type** | `Microsoft.CognitiveServices/accounts` (kind: `AIServices`) |

### 1.4 RBAC Roles

| Role | Scope | Capabilities |
|------|-------|-------------|
| **Foundry User** | Resource/Project | Use agents, models, tools |
| **Foundry Account Owner** | Resource | Full resource administration, deploy models |
| **Foundry Project Manager** | Resource | Create projects, assign Foundry User to team members |
| **Foundry Owner** | Resource | Full access including data plane |

> **Note:** These roles were recently renamed from "Azure AI User", "Azure AI Account Owner", etc. The role IDs and permissions are unchanged.

### 1.5 Authentication

- **Recommended:** Microsoft Entra ID with `DefaultAzureCredential`
- **Alternative:** API keys (for quick demos only — not production)
- **Managed Identity:** For production workloads and service-to-service

---

## Demo: Create Foundry Resource & Deploy Model

### Pre-Demo Setup Checklist

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Azure subscription active | Azure Portal | Can create resources |
| 2 | Sufficient quota for GPT-4.1 | Azure Portal → Quotas | TPM available in chosen region |
| 3 | New Foundry toggle ON | ai.azure.com | See "New Foundry" in banner |

### Demo Steps

**Step 1: Create a Foundry Project**

1. Open [ai.azure.com](https://ai.azure.com) → ensure **New Foundry** toggle is ON
2. Click project selector → **Create new project**
3. Enter project name: `contoso-estimator`
4. Advanced options:
   - Resource group: `rg-contoso-foundry-workshop`
   - Location: Select available region with GPT-4.1 quota
5. Click **Create** — provisions Foundry resource + project automatically

**Step 2: Deploy GPT-4.1**

1. Navigate to **Discover** → **Model catalog**
2. Search for `gpt-4.1`
3. Click **Deploy** → Configure:
   - Deployment name: `gpt-4-1`
   - Deployment type: Global Standard (best availability)
   - TPM: 30K (sufficient for workshop demos)
4. Click **Deploy**

**Step 3: Test in Playground**

1. Navigate to **Build** → **Models** → select `gpt-4-1`
2. In playground, send: "What is a bill of quantities in construction?"
3. Verify response — model is working

**Step 4: Note Project Endpoint**

Copy the project endpoint from the Home page — needed for SDK usage later:
```
https://<foundry-resource-name>.services.ai.azure.com/api/projects/contoso-estimator
```

---

## Key Takeaways

1. Foundry V2 uses a flat **resource → project** hierarchy (no more hubs)
2. Models are deployed at the **resource level** and shared across projects
3. RBAC separates admin (resource) from developer (project) concerns
4. The same agent can be built via portal OR SDK — both use the same API

---

## What's Next

In **Module 2**, we'll create the **Contoso Estimator Advisor** agent in this project using File Search and Code Interpreter.

---

## References

| Resource | Link |
|----------|------|
| What is Foundry? | https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio |
| Create Foundry Resources | https://learn.microsoft.com/azure/foundry/tutorials/quickstart-create-foundry-resources |
| Foundry Architecture | https://learn.microsoft.com/azure/foundry/concepts/architecture |
| RBAC in Foundry | https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry |
| Migrate from Classic | https://learn.microsoft.com/azure/foundry/how-to/navigate-from-classic |
