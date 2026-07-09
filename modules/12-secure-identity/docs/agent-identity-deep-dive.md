# Agent Identity — Deep Dive

> Companion to [Module 12](../README.md). Verified against Microsoft Learn, July 2026.

## What an agent identity is

An **agent identity** is a specialized Microsoft Entra ID service principal that represents an AI agent at runtime. Foundry provisions and manages it across the agent lifecycle. It exists to solve identity problems unique to agents:

- Distinguish **agent** actions from user, workforce, or workload actions in audit logs.
- Give agents **right-sized** (least-privilege) access instead of a shared, over-scoped identity.
- Prevent agents from acquiring critical security roles.
- Scale identity management to many short-lived agents.

## Blueprint vs identity

| Term | Meaning |
|------|---------|
| **Agent identity blueprint** | An Entra object that governs a *class* of agents and performs lifecycle operations (create/update/delete identities). |
| **Agent identity** | The service principal for a *specific* agent, created and impersonated via the blueprint. |
| `agentIdentityId` | The identifier used when assigning RBAC to the agent identity (from the project/agent JSON view). |

The blueprint holds OAuth credentials (client secret, certificate, or **federated credential / managed identity**). Federated credentials are preferred — no stored secrets, automatic rotation.

> The managed identity authenticates the **blueprint** to Entra. It does **not** access the downstream resource. The **agent identity** is the principal that needs RBAC on the target.

## Unpublished vs published

| State | Identity used for tool calls | RBAC action |
|-------|------------------------------|-------------|
| **Unpublished** | Shared **project** identity | Grant on project identity |
| **Published** (Agent Application) | **Distinct** agent identity | **Re-assign** roles to the new identity on every downstream resource |

Publishing requires **Foundry Project Manager**. Re-assigning RBAC to the published identity requires **Owner** or **User Access Administrator** on the target resource.

## Runtime token exchange (4 stages)

```mermaid
sequenceDiagram
    participant AS as Agent Service
    participant AAD as Microsoft Entra ID
    participant R as Downstream resource
    AS->>AAD: 1. Blueprint authentication (OAuth creds)
    AAD-->>AS: 2. Agent identity token
    AS->>AAD: 3. Request token scoped to resource audience
    AAD-->>AS: scoped access token
    AS->>R: 4. Tool call with scoped token
```

Developers never handle these tokens — Agent Service performs the exchange.

## Assigning RBAC to an agent identity

```azurecli
# Grant read-only blob access to the agent identity
az role assignment create \
  --assignee-object-id "<agentIdentityId>" \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<sa>"

# Verify
az role assignment list --assignee "<agentIdentityId>" \
  --scope "/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<sa>" -o table
```

`--assignee-object-id` + `--assignee-principal-type ServicePrincipal` avoids Microsoft Graph lookup errors for agent-identity service principals.

## Audience values

| Downstream service | Audience |
|--------------------|----------|
| Azure Storage | `https://storage.azure.com` |
| Azure Cosmos DB | `https://cosmos.azure.com` |
| Azure Key Vault | `https://vault.azure.net` |
| Azure Logic Apps | `https://logic.azure.com` |
| Microsoft Graph | `https://graph.microsoft.com` |
| Azure AI Search (delegated) | `https://search.azure.com` |

An incorrect audience fails authentication **even when RBAC is correct**.

## Limitations & common failures

- Only some tools support agent-identity auth today — check the tool's docs.
- **Roles on the wrong identity** — after publishing, roles on the project identity don't transfer.
- **Missing role** on the target resource.
- **Wrong audience** for the downstream service.

## References

- [Agent identity concepts](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-identity)
- [Elevated-role tasks (publish / reassign)](https://learn.microsoft.com/azure/foundry/concepts/administrator-guide)
- [Manage hosted agent — retrieve identity](https://learn.microsoft.com/azure/foundry/agents/how-to/manage-hosted-agent)
- [Agent-to-agent authentication](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-to-agent-authentication)
