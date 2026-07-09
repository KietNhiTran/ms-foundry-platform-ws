# Data Protection & Customer-Managed Keys — Deep Dive

> Companion to [Module 13](../README.md). Verified against Microsoft Learn, July 2026.

## Default encryption

All data at rest and in transit is encrypted by default with **Microsoft-managed keys** using FIPS 140-2-compliant **256-bit AES**. It's transparent — no code or config changes required.

## Customer-managed keys (CMK / BYOK)

CMK gives you control to **create, rotate, disable, revoke, and audit** the keys protecting your data. When configured, you get **double encryption** — a second layer you control via Azure Key Vault or Managed HSM.

**Scope:** data at rest in the Foundry resource's associated storage — **project artifacts, uploaded files, evaluation data**.

### Key Vault prerequisites

| Requirement | Detail |
|-------------|--------|
| Region & tenant | Key Vault in the **same region and tenant** as the Foundry resource (subscriptions may differ) |
| Recovery | **Soft delete** and **purge protection** both enabled |
| Key | RSA or RSA-HSM, **2048** |
| Permission (RBAC) | **Key Vault Crypto User** on the resource's managed identity (get / wrap / unwrap) |
| Availability | **Select regions only** (Azure AI Search capacity) — verify first |

> If soft delete / purge protection aren't enabled and the key is deleted, the encrypted data is **unrecoverable**.

### Key store networking (with private Foundry)

When the Foundry resource uses private networking, the Key Vault / Managed HSM hosting the CMK supports:

1. **Private endpoint + "Allow trusted Microsoft services"** — recommended for private environments.
2. **"Allow trusted Microsoft services" only** (public endpoint) — permits Foundry to reach the key store for crypto operations.

### Portal flow

**Foundry resource → Encryption → Encryption type → Customer Managed Keys** → select the Key Vault + key.

## Data residency & storage options

| Setup | Data location | Isolation |
|-------|---------------|-----------|
| **Basic agent** | Microsoft-managed multitenant storage | Logical separation |
| **Standard agent (BYO)** | Your Azure Storage / Cosmos DB / AI Search | **Per-project** isolation in your subscription |

For "our data never leaves our subscription / region", use the **standard/BYO** setup and pin the region. Foundry tools (evaluations, batch) read/write to your BYO storage.

## Rotation & revocation story

- **Rotate:** create a new key version in Key Vault; Foundry uses the latest.
- **Revoke:** disable the key or remove the resource identity's access → data becomes inaccessible (intended kill-switch).
- **Audit:** Key Vault logs every key operation.

## References

- [Customer-managed keys for Foundry](https://learn.microsoft.com/azure/foundry/concepts/encryption-keys-portal)
- [Foundry architecture — data storage](https://learn.microsoft.com/azure/foundry/concepts/architecture#data-storage)
- [Bring your own resources with the Agent service](https://learn.microsoft.com/azure/foundry/agents/how-to/use-your-own-resources)
- [Azure Key Vault recovery (soft delete & purge protection)](https://learn.microsoft.com/azure/key-vault/general/key-vault-recovery)
- [Azure security baseline for Foundry — Data protection](https://learn.microsoft.com/security/benchmark/azure/baselines/azure-ai-foundry-security-baseline)
