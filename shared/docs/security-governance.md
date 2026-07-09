# Foundry Security & Governance — Reference Appendix

> **Reference only — not a live demo module.** These Azure-side and platform controls complement the hands-on Security Track ([Module 12 — Secure Agent Identity & Access](../../modules/12-secure-identity/README.md), [Module 13 — Network Isolation & Data Protection](../../modules/13-network-data-protection/README.md)) and the runtime-security modules (Module 4 Content Safety, Module 9 Red Teaming). Use as talking points and follow-up reading.
>
> Verified against Microsoft Learn, July 2026. Terminology follows the current Foundry terms.

---

## Security domain map

The full Foundry security surface spans eight domains. The workshop covers them as follows:

| Domain | Where it's covered |
|--------|--------------------|
| Identity & access (RBAC, agent identity, OBO) | **Module 12** (hands-on) |
| Network isolation | **Module 13** (hands-on) |
| Data protection (CMK, residency, BYO) | **Module 13** (hands-on) |
| Content safety / Responsible AI runtime guardrails | **Module 4** (hands-on) |
| Red teaming & safety testing | **Module 9** (hands-on) |
| Observability for security (audit/tracing) | **Module 6** (hands-on) |
| Threat protection & posture | *This appendix* |
| Governance & compliance | *This appendix* |

---

## 1. Microsoft Defender for Cloud — AI threat protection

Foundry integrates with Defender for Cloud's **AI workload** protections.

- **Portal:** project → **Risks + alerts** → review alerts and posture recommendations.
- **Detects:** jailbreak / user-prompt attacks, misconfigurations, unusual model-usage patterns, threats to AI workloads.
- **Layered story:** Prompt Shields detect at runtime → content filters block output → Defender alerts on the incident for SecOps.

📖 [Alerts for AI workloads](https://learn.microsoft.com/azure/defender-for-cloud/alerts-ai-workloads)

## 2. Azure security baseline (MCSB)

The **Azure security baseline for Foundry** maps Foundry controls to the Microsoft Cloud Security Benchmark (network security, identity, data protection, logging, posture). Use it as the checklist for a production security review.

> Note: sensitive-data discovery/classification and native DLP are **not** built into Foundry — protect data via network isolation, access control, and the Purview integration below.

📖 [Azure security baseline for Foundry](https://learn.microsoft.com/security/benchmark/azure/baselines/azure-ai-foundry-security-baseline)

## 3. Microsoft Purview integration *(reference only — not demoed)*

For data governance and DLP over AI interactions:

| Capability | Purpose |
|-----------|---------|
| Auditing | Track interactions with Foundry resources |
| SIT classification | Detect Sensitive Information Types in prompts/responses |
| DSPM for AI | Data Security Posture Management over AI data flows |
| DLP policies | Reduce sensitive-data leakage via agent interactions |

> Purview Data Security **policies require Entra ID user-context tokens** (delegated/OBO) — this is why Module 12's OBO pattern matters for governance. Left as reference per scope.

📖 [Manage compliance and security in Foundry](https://learn.microsoft.com/azure/foundry/control-plane/how-to/how-to-manage-compliance-security)

## 4. Azure Policy governance

Use Azure Policy to enforce platform guardrails that identity/network controls can't, e.g.:

- **Block public-endpoint tools** (Bing / Web Search / SharePoint) where disallowed.
- Restrict which models/regions may be deployed.
- Require private networking / disabled public network access on Foundry resources.

## 5. Conditional Access

Tenant admins can gate access to Foundry surfaces (including the Foundry MCP Server app) with Conditional Access — device, location, risk, and MFA conditions on users or workload identities.

📖 [Foundry MCP Server security & governance](https://learn.microsoft.com/azure/foundry/mcp/security-best-practices)

## 6. Compliance documentation — AI Reports

**AI Reports** document a project for GRC / audit workflows — model cards & versions, content-filter configuration, and evaluation metrics — exportable as **PDF** or **SPDX**. Pair with Module 6 (tracing/audit) and Module 7 (evaluation) evidence.

## 7. Responsible AI framework — Discover / Protect / Govern

| Stage | Controls | Workshop module |
|-------|----------|-----------------|
| **Discover** | Adversarial testing, AI Red Teaming Agent, safety evaluators | Module 9 |
| **Protect** | Content filters, Prompt Shields, custom guardrails, system-prompt boundaries | Module 4 |
| **Govern** | Continuous monitoring/evaluation, AI Reports, Defender posture | Modules 6, 7 + this appendix |

📖 [Responsible AI for Microsoft Foundry](https://learn.microsoft.com/azure/foundry/responsible-use-of-ai-overview)

## 8. Data access security standards

Approved paths for connecting agents to data:

| Path | Security model | Best for |
|------|----------------|----------|
| Foundry IQ (Knowledge Bases) | Azure AI Search with sensitivity labels / ACLs / permission filters | Consistent enforcement across agents (see Module 3 RLS) |
| Fabric Data Agents | Fabric fine-grained security | Sensitive organisational data |
| Direct connections (MCP, APIs) | Per-connection auth (OAuth / managed identity / keys) | Real-time queries (see Module 12 agent identity) |

📖 [Data security standards for AI — Foundry](https://learn.microsoft.com/azure/cloud-adoption-framework/data/operational-standards-data-product-security-standards-unify-data-platform)

## 9. Business continuity / disaster recovery

Not a security module, but part of resilience posture. See the reference material in [ref/handy-qa/07-disaster-recovery.md](../../ref/handy-qa/07-disaster-recovery.md) for multi-region failover and backup strategy.

---

## Out of scope for this workshop

Per delivery scope, the following are **reference only** and not demonstrated live:

- Microsoft Purview DLP hands-on integration
- Microsoft 365 / Agent 365 publishing and governance
- Regulatory compliance mappings (HIPAA / PCI-DSS / SOC 2)
- Incident-response playbooks and secrets-rotation automation
