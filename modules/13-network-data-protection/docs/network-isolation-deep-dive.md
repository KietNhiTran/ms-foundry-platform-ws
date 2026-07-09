# Network Isolation — Deep Dive

> Companion to [Module 13](../README.md). Verified against Microsoft Learn, July 2026.

## The three areas

1. **Inbound** — user/app → Foundry resource. Controlled by a **private endpoint** and the **Public Network Access (PNA)** flag (`Disabled` to require private access).
2. **Outbound (PaaS)** — Foundry → Storage, Key Vault, AI Search, Cosmos DB via **Private Link**.
3. **Outbound (agent client)** — agent runtime → data sources / APIs / internet, kept in-boundary by **VNet injection** or a **Managed VNet**.

## Option A — Custom VNet (BYO), GA

Full control over topology and routing.

**Requirements**
- Public Network Access = **Disabled**; private endpoint to the Foundry resource.
- **Bring Your Own** Storage, AI Search, Cosmos DB (required for VNet injection).
- Subnet delegated to `Microsoft.App/environments`, size **`/27` or larger**.
- Separate private endpoints to Storage, AI Search, Cosmos DB.

**Portal flow**
1. Create Foundry resource → **Storage** tab → choose **your own** Storage / AI Search / Cosmos DB.
2. **Networking** → Public access **Disabled**.
3. Add **Private endpoint** (same region as the VNet).
4. Set **Virtual Network Injection** → your VNet + delegated subnet.
5. Create private endpoints to the dependencies on their own resource pages.

## Option B — Managed VNet, Preview

Microsoft provisions and manages the VNet. Deployed via **Bicep only**.

**Isolation modes**

| Mode | Behaviour | Use when |
|------|-----------|----------|
| Allow Internet Outbound | All egress allowed; inbound via private endpoints | Broad connectivity OK |
| Allow Only Approved Outbound | Egress limited to approved service tags, private endpoints, FQDN rules | Minimize exfiltration |
| Disabled | No managed network | Use BYO VNet or public |

**Key facts**
- Managed private endpoints to dependencies (Storage auto; Cosmos DB & AI Search via CLI).
- FQDN outbound rules: ports **80/443** only, via a managed Azure Firewall (extra cost).
- **Cannot downgrade** the isolation mode once set.
- Supported regions include **Australia East**, East US/US2, West Europe, and others.

## Tool traffic classes

| Class | Tools | Networking |
|-------|-------|-----------|
| Microsoft backbone | Code Interpreter, Function Calling | None |
| Private endpoint | File Search (Storage), Azure AI Search, private MCP | Private endpoint to the dependency |
| Public endpoint | Bing / Web Search, SharePoint | Public egress — block with **Azure Policy** if disallowed |

**AI Search indexers:** if ingestion traverses private endpoints, set the indexer `executionEnvironment` to `Private`. Otherwise it defaults to multitenant execution, can't reach private endpoints, and silently produces an empty index. Import-data-wizard and auto-generated indexers for indexed knowledge sources don't support the private execution environment.

## Blocking public tools with Azure Policy

Where public-endpoint tools (Bing, SharePoint) are not permitted, use Azure Policy to deny their use rather than relying on convention. This complements network isolation for tools whose traffic can't be forced onto a private endpoint.

## References

- [Configure private link for Foundry](https://learn.microsoft.com/azure/foundry/how-to/configure-private-link)
- [Managed virtual network](https://learn.microsoft.com/azure/foundry/how-to/managed-virtual-network)
- [Indexer access to network-protected content](https://learn.microsoft.com/azure/search/search-indexer-securing-resources)
- [Azure security baseline for Foundry — Network security](https://learn.microsoft.com/security/benchmark/azure/baselines/azure-ai-foundry-security-baseline)
