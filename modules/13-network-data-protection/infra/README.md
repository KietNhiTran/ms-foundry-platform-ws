# Module 13 Infrastructure — Foundry Sample Template 15

This module does **not** ship its own private-networking Bicep. It deploys the
**official Microsoft Foundry sample**:

> **Template 15 — Standard Agent Setup with E2E Network Isolation (without Tools behind VNet)**
> [`foundry-samples/infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup`](https://github.com/microsoft-foundry/foundry-samples/tree/main/infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup)

Using the maintained sample keeps the demo aligned with the current Foundry
network-isolation reference architecture (capability host, AMPLS, DNS zones,
RBAC) instead of a hand-rolled copy that drifts out of date.

## Files

| File | Purpose |
|------|---------|
| `main.bicepparam` | Workshop parameter values (Contoso, `eastus2`, new VNet, BYO resources) copied into the downloaded template at deploy time. |
| `deploy.ps1` | Sparse-clones template 15, stages `main.bicepparam`, and runs `az deployment group create`. |
| `teardown.ps1` | Deletes the resource group **and purges** the Foundry account so the delegated agent subnet unlinks (required by the template's cleanup guidance). |
| `.template-15/` | Local sparse checkout of the sample (created by `deploy.ps1`; git-ignored). |

## What gets deployed

A fresh, self-contained network-secured environment in a **dedicated resource
group** (`rg-contoso-foundry-secured` by default), leaving the public Module 1
resource untouched:

- New VNet (`192.168.0.0/16`) with an **agent subnet** delegated to
  `Microsoft.App/environments` and a **private-endpoint subnet**
- Network-secured **Foundry account** (public network access **Disabled**) + project
- **BYO** Storage / Cosmos DB / AI Search behind private endpoints (data residency)
- **Azure Monitor Private Link Scope (AMPLS)** for private telemetry ingestion
- Private DNS zones + capability host (created implicitly via
  `networkInjections.scenario='agent'`)

## Deploy

```powershell
# Register providers once per subscription (see template prerequisites)
az provider register --namespace Microsoft.CognitiveServices
az provider register --namespace Microsoft.Search
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.Network
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.ContainerService

./deploy.ps1 -ResourceGroup rg-contoso-foundry-secured -Location eastus2
```

Private endpoints + VNet injection typically take **15–25 min**. The Foundry
resource has **no public access** — reach it from inside the VNet via a jump
box, VPN Gateway, or Azure Bastion.

## Teardown

```powershell
./teardown.ps1 -ResourceGroup rg-contoso-foundry-secured -Location eastus2
```

> The account must be **purged** (not just deleted) or the agent subnet stays
> linked to the capability host and can't be reused. Allow ~20 min to fully
> unlink. For complex cases the sample also ships a
> [cleanup tool](https://github.com/microsoft-foundry/foundry-samples/blob/main/infrastructure/infrastructure-setup-bicep/deployment-tools/cleanup/README.md).

## Customizing

Edit `main.bicepparam`. Common changes:

- **Reuse an existing VNet:** set `existingVnetResourceId` and the subnet names/prefixes.
- **Reuse the Module 1 Foundry account:** add `existingAiFoundryAccountResourceId`
  and `skipModelDeployment = true` (see the template README's *BYO Resource Details*).
- **Bring existing Storage/Cosmos/Search:** set the matching `*ResourceId` params.
- **Enable ACR:** set `enableContainerRegistry = true` (needed for custom container tools).

See the [template README](https://github.com/microsoft-foundry/foundry-samples/tree/main/infrastructure/infrastructure-setup-bicep/15-private-network-standard-agent-setup)
for the full parameter reference and networking requirements.
