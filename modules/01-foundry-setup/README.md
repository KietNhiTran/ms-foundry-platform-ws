# Module 1: Foundry Platform Overview & Setup

**Duration:** 45 minutes  
**Format:** Led Demo (Portal)

**Objective:** Understand Microsoft Foundry's architecture, provision resources, and deploy a foundation model.

---

## Topics

- What is Microsoft Foundry — unified platform for agents, models, and tools
- Evolution: Azure AI Studio → Azure AI Foundry → Microsoft Foundry
- V2 architecture: Foundry resource → projects (flat resource model)
- Model Catalog — 1,900+ models (GPT-5, GPT-4.1, Claude, Mistral, Llama, and more)
- Resource provisioning and project creation
- Model deployment (GPT-4.1 recommended for production workloads)
- RBAC roles: Foundry User, Foundry Account Owner, Foundry Project Manager
- Entra ID authentication and managed identities

---

## Concepts

### What Is Microsoft Foundry?

Microsoft Foundry is the **unified Azure platform-as-a-service** for enterprise AI — bringing models, agents, tools, observability, and governance under a single management surface.

> **Reference:** [What is Microsoft Foundry?](https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio)

### Platform Evolution

| Era | Name | Key Change |
|-----|------|------------|
| 2023 | Azure AI Studio | Initial preview — portal for model experimentation |
| 2024 | Azure AI Foundry | GA rebrand — added agent service, tool catalog |
| 2025–2026 | **Microsoft Foundry** | V2 flat resource model, unified Foundry resource, control plane |

### V2 Resource Model

Foundry V2 simplifies the Azure AI resource hierarchy:

```
Foundry Resource (AI Services account)
├── Model Deployments (GPT-4.1, Phi-4, Llama, etc.)
├── Hub
│   └── Project ("contoso-estimator")
│       ├── Agents
│       ├── Playground
│       ├── Evaluations
│       └── Connections
└── Built-in capabilities
    ├── Content Safety
    ├── Speech, Vision, Language
    └── Document Intelligence
```

A **Foundry resource** (formerly Azure AI Services) is the top-level account. **Hubs** organize projects and shared settings. **Projects** are workspaces where you build agents, run evaluations, and manage deployments.

### Model Catalog

The Foundry Model Catalog provides access to **1,900+ models** from leading providers:

| Provider | Example Models |
|----------|---------------|
| OpenAI | GPT-4.1, GPT-4o, o3, o4-mini |
| Microsoft | Phi-4, Phi-4-mini, MAI |
| Meta | Llama 4 Scout, Llama 4 Maverick |
| Mistral AI | Mistral Large, Codestral |
| DeepSeek | DeepSeek-R1 |
| Cohere | Command A |

Models can be deployed as **Global Standard** (pay-per-token, serverless) or **Provisioned** (reserved capacity) depending on workload requirements.

### RBAC Roles

| Role | Scope | Allows |
|------|-------|--------|
| **Foundry User** | Resource / Project | Use models, run agents, access playground |
| **Foundry Account Owner** | Resource | Manage deployments, connections, billing |
| **Foundry Project Manager** | Project | Manage project members and settings |

> **Note:** These roles are being renamed from "Azure AI User", "Azure AI Account Owner", etc. The role IDs and permissions remain unchanged.

### Authentication

Microsoft Foundry uses **Microsoft Entra ID** (formerly Azure AD) as the primary authentication method:

- **Interactive login** — for portal and local development
- **Managed identities** — for production workloads (recommended)
- **Service principals** — for CI/CD pipelines
- Key-based auth is disabled by default in the Bicep template (security best practice)

---

## Pre-Demo Setup Checklist

Complete these steps **before** presenting the demo.

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Azure subscription with Foundry access | Ensure your subscription can create `Microsoft.CognitiveServices` resources | Azure portal → Subscriptions → Resource providers |
| 2 | Assign yourself **Owner** or **Contributor** on the subscription | Azure portal → Subscriptions → Access control (IAM) | `az role assignment list --assignee <your-email>` |
| 3 | Install Azure CLI | [Install Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | `az --version` |
| 4 | Login to Azure | `az login` | `az account show` |
| 5 | Run the deploy script | See [Infrastructure Deployment](#infrastructure-deployment) below | Outputs display endpoint and resource IDs |
| 6 | Verify in portal | Open [ai.azure.com](https://ai.azure.com) → select your project | Project and GPT-4.1 deployment visible |
| 7 | Test playground | Portal → Playground → select `gpt-4.1` → send a test message | Model responds successfully |

---

## Infrastructure Deployment

This module includes a Bicep template that provisions all required resources.

### What Gets Created

| Resource | Type | Purpose |
|----------|------|---------|
| Foundry resource | `Microsoft.CognitiveServices/accounts` (AIServices) | AI Services account with models endpoint |
| GPT-4.1 deployment | `Microsoft.CognitiveServices/accounts/deployments` | Chat completion model |
| Storage account | `Microsoft.Storage/storageAccounts` | Hub workspace storage |
| Key Vault | `Microsoft.KeyVault/vaults` | Secrets management |
| AI Hub | `Microsoft.MachineLearningServices/workspaces` (Hub) | Organizes projects |
| AI Project | `Microsoft.MachineLearningServices/workspaces` (Project) | Workshop workspace |
| Hub → Foundry connection | `Microsoft.MachineLearningServices/workspaces/connections` | Links hub to model deployments |
| RBAC assignment | `Microsoft.Authorization/roleAssignments` | Foundry User role (optional) |

### Deploy

```powershell
# From modules/01-foundry-setup/infra/
./deploy.ps1 -ResourceGroupName "rg-foundry-workshop" `
             -AccountName "foundryws2026" `
             -Location "eastus2"
```

To also assign the Foundry User role to a specific user:

```powershell
./deploy.ps1 -ResourceGroupName "rg-foundry-workshop" `
             -AccountName "foundryws2026" `
             -PrincipalId "<entra-object-id>"
```

### Tear Down

```powershell
# From modules/01-foundry-setup/infra/
./teardown.ps1 -ResourceGroupName "rg-foundry-workshop"
```

---

## Demo: Create Foundry Resource, Deploy GPT-4.1, Run First Chat Completion

> **Format:** Portal-led demo (~20 minutes)  
> **Prerequisite:** Infrastructure deployed via `deploy.ps1` above

### Part 1 — Explore the Foundry Resource (5 min)

1. Open [ai.azure.com](https://ai.azure.com) and sign in
2. Show the **Management center** — point out the Foundry resource, connected hub, and project
3. Navigate to **Models + endpoints** and show the GPT-4.1 deployment
4. Highlight the flat resource model: Foundry resource → Hub → Project
5. Show **Access control (IAM)** in the Azure portal — point out the Foundry RBAC roles

### Part 2 — Explore the Model Catalog (5 min)

1. Navigate to **Model catalog** in the Foundry portal
2. Filter by task: Chat completion, Embeddings, Image generation
3. Show model cards — note provider, benchmarks, and deployment options
4. Compare deployment types: Global Standard vs Provisioned vs Serverless (Direct Models)
5. Highlight that adding Global Standard deployments is pay-per-token with no upfront cost

### Part 3 — First Chat Completion in Playground (10 min)

1. Navigate to **Playground** → **Chat**
2. Select the `gpt-4.1` deployment
3. Set the **system message**:

   ```
   You are the Contoso Estimator Advisor, an AI assistant for Contoso
   Infrastructure. You help estimators prepare project bids by answering
   questions about construction rates, costs, and estimation best practices.
   Be concise and professional.
   ```

4. Send a test message:

   ```
   What factors should I consider when estimating earthworks costs for
   a highway project?
   ```

5. Show the response — point out token usage and latency in the response metadata
6. Discuss: this is the foundation model we will enhance with tools, knowledge, and guardrails throughout the workshop

> **Pro-code equivalent:** See Module 11, Step 1 — `Step01_FirstApiCall.cs`

---

## Key Takeaways

1. **Microsoft Foundry** is the unified platform for enterprise AI — models, agents, tools, and governance in one place
2. The **V2 flat resource model** simplifies provisioning: one Foundry resource serves models, with hubs and projects for organization
3. The **Model Catalog** provides 1,900+ models with flexible deployment options (pay-per-token or provisioned)
4. **Entra ID** is the recommended authentication method; key-based auth should be disabled for production
5. Infrastructure-as-code (Bicep) enables repeatable, auditable deployments

---

## References

| Resource | Link |
|----------|------|
| What is Microsoft Foundry? | [learn.microsoft.com/azure/ai-studio/what-is-ai-studio](https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio) |
| Create Foundry Resources | [learn.microsoft.com/azure/foundry/tutorials/quickstart-create-foundry-resources](https://learn.microsoft.com/azure/foundry/tutorials/quickstart-create-foundry-resources) |
| Foundry Resource Bicep Quickstart | [learn.microsoft.com/azure/foundry/foundry-models/how-to/quickstart-create-resources](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/quickstart-create-resources?pivots=programming-language-bicep) |
| Model Catalog | [learn.microsoft.com/azure/foundry/model-catalog/model-catalog-overview](https://learn.microsoft.com/azure/foundry/model-catalog/model-catalog-overview) |
| RBAC Roles | [learn.microsoft.com/azure/foundry/concepts/rbac-foundry](https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry) |
| Migrate from Classic | [learn.microsoft.com/azure/foundry/how-to/navigate-from-classic](https://learn.microsoft.com/azure/foundry/how-to/navigate-from-classic) |
