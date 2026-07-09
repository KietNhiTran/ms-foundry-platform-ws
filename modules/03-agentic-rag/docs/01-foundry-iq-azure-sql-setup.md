# Step 1 · Demo Resource Setup — Foundry IQ + Azure SQL

> **📍 Step 1 of 4** · [🧭 Overview](00-foundry-iq-azure-sql-rls.md) · ➡ Next: [Step 2 · SQL knowledge source + agent](02-foundry-iq-azure-sql.md)

**Version:** 1.0
**Last Updated:** July 2026
**Applies to:** the Azure SQL knowledge-source deep-dive and both row-level access
control (RLS) tracks in this folder.

> Shared, one-time setup for the Azure SQL rate-library demos. Complete this
> **before** running:
> - [Base: Rate library on Azure SQL](02-foundry-iq-azure-sql.md)
> - [Track 1: Row-level access with security filters](03-foundry-iq-rls-security-filter.md)
> - [Track 2: Identity-native row-level security](04-foundry-iq-rls-identity.md)

---

## What you will provision

| # | Resource | Purpose |
|---|----------|---------|
| 1 | Azure SQL Database + `dbo.rate_library` table | Structured rate data (source of truth) |
| 2 | Azure AI Search (Basic) with system-assigned managed identity | Hosts the knowledge base + generated index |
| 3 | Embedding + chat models in the Foundry project | Vectorization + agentic retrieval (from Module 1) |
| 4 | RBAC role assignments | Let Search read SQL / call embeddings; let the project + apps query |
| 5 | Entra security groups + test users | Personas for the RLS tracks |
| 6 | Entra app registration | Sign-in + token acquisition for the consuming apps |

> **Terminology:** *Foundry resource* = the account that hosts your models;
> *Foundry IQ* = the managed knowledge layer on Azure AI Search. See the
> [terminology table](../../../.github/copilot-instructions.md).

---

## Prerequisites

- Module 1 complete: a Foundry resource + project with **gpt-5-4** and
  **text-embedding-3-small** deployed.
- Azure CLI signed in to the workshop tenant:
  ```powershell
  az login --tenant <your-tenant-id>
  az account set --subscription <your-subscription-id>
  ```
- The [Azure AI Search 2026-05-01-preview features](https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-migrate)
  are preview — use a region that supports [agentic retrieval](https://learn.microsoft.com/azure/search/search-region-support).

Set reusable variables (PowerShell):

```powershell
$RG          = "rg-contoso-foundry-workshop"
$LOCATION    = "australiaeast"
$SQL_SERVER  = "contoso-estimator-sql"          # must be globally unique
$SQL_DB      = "contoso-rates"
$SEARCH      = "contoso-foundry-ws-srch"          # must be globally unique
$FOUNDRY_ACCT= "contoso-foundry-ws-resource"     # your Foundry resource name
```

---

## 1. Azure SQL Database + rate table

```powershell
# SQL logical server (Entra-only auth recommended; add a SQL admin for the demo if needed)
az sql server create `
  --name $SQL_SERVER --resource-group $RG --location $LOCATION `
  --enable-ad-only-auth `
  --external-admin-principal-type User `
  --external-admin-name "admin@yourtenant.onmicrosoft.com" `
  --external-admin-sid (az ad signed-in-user show --query id -o tsv)

# Serverless General Purpose database (cheap for a demo)
az sql db create `
  --name $SQL_DB --server $SQL_SERVER --resource-group $RG `
  --edition GeneralPurpose --compute-model Serverless `
  --family Gen5 --capacity 1 --auto-pause-delay 60

# Allow Azure services + your client IP through the firewall
az sql server firewall-rule create --resource-group $RG --server $SQL_SERVER `
  --name AllowAzure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
az sql server firewall-rule create --resource-group $RG --server $SQL_SERVER `
  --name AllowMyClient --start-ip-address <your-ip> --end-ip-address <your-ip>
```

Seed the table with [`../data/rate-library.sql`](../data/rate-library.sql):

```powershell
# Using sqlcmd with Entra auth (Microsoft.Data.SqlClient / sqlcmd v0.21+)
sqlcmd -S "$SQL_SERVER.database.windows.net" -d $SQL_DB -G `
  -i "../data/rate-library.sql"
```

**Verify:** the final query prints 4 rows (NSW/VIC/QLD/WA) with `156` total rows.

> **Change tracking (required):** the seed script enables SQL integrated change
> tracking on the database and table (the `CHANGE_TRACKING` block at the bottom).
> The Foundry IQ Azure SQL knowledge source uses the SQL integrated
> change-tracking policy for tables, so creating it fails with *"Integrated
> change tracking is not enabled for table '&lt;name&gt;'"* if this hasn't run.
> If you seeded before this was added, run just those two `ALTER` statements as
> the SQL Entra admin, then retry the knowledge source.

### 1a. Managed-identity ingestion (recommended)

The Foundry portal's Azure SQL knowledge source shows a **Connection string** field,
but you can make the indexer authenticate with the **search service's managed
identity** instead of a password — no secret stored. This needs **three** pieces,
all of them required
([managed-identity SQL docs](https://learn.microsoft.com/azure/search/search-howto-managed-identities-sql)):

| # | Piece | Layer | Purpose |
|---|-------|-------|---------|
| 1 | **SQL DB Contributor** (or SQL Server Contributor) on the SQL server | Control plane (ARM) | Search resolves the server endpoint from the resource ID |
| 2 | `CREATE USER … db_datareader` | Data plane (SQL) | Search reads the table rows |
| 3 | **`ResourceId=` connection string** | Data source | Format that triggers MI auth — **not** `Server=tcp:…` |

**Piece 1 — role assignment on the SQL logical server (control plane).** Without it
you get *"Unable to retrieve server endpoint … grant permission (e.g., Reader) to
read server endpoint"*:

```powershell
$SEARCH_MI     = az search service show --name $SEARCH --resource-group $RG --query "identity.principalId" -o tsv
$SQL_SERVER_ID = az sql server show --name $SQL_SERVER --resource-group $RG --query id -o tsv
az role assignment create --assignee $SEARCH_MI --role "SQL DB Contributor" --scope $SQL_SERVER_ID
```

**Piece 2 — a read-only DB user (data plane).** Connect to the
database as the SQL Entra admin (the signed-in user from step 1) and run — the
principal name is the **search service name** (its system-assigned identity shares
that name):

```sql
-- run against the contoso-rates database
CREATE USER [contoso-foundry-ws-srch] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [contoso-foundry-ws-srch];
```

```powershell
# One way to run it (interactive Entra auth):
sqlcmd -S "$SQL_SERVER.database.windows.net" -d $SQL_DB -G -Q `
  "CREATE USER [$SEARCH] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [$SEARCH];"
```

**Piece 3 — the `ResourceId=` connection string.** For managed-identity auth,
Azure AI Search **rejects** the ordinary `Server=tcp:…` form and needs the
credential-less **`ResourceId=`** format (no host, no `User ID`/`Password`). Using
the wrong form is the usual cause of *"Failed to connect to SQL database."* Paste
this into the knowledge-source **Connection string** field:

```
Database=contoso-rates;ResourceId=/subscriptions/<subscription-id>/resourceGroups/rg-contoso-foundry-workshop/providers/Microsoft.Sql/servers/contoso-estimator-sql;Connection Timeout=30;
```

Build it from your own values with:

```powershell
"Database=$SQL_DB;ResourceId=$SQL_SERVER_ID;Connection Timeout=30;"
```

> - Works because the search service has a **system-assigned managed identity**
>   (step 2) and the `AllowAzure` firewall rule (step 1).
> - **Propagation:** after the role grant, allow **up to ~30 min** before the create
>   succeeds. Azure AI Search caches its MI token *and* failed endpoint lookups, so an
>   early retry can keep returning *"Unable to retrieve server endpoint … granted
>   permission (e.g., Reader)"* even though the role is already assigned. Verify the
>   grant landed on the current identity with
>   `az role assignment list --assignee $SEARCH_MI --scope $SQL_SERVER_ID -o table`,
>   then wait and retry. To unblock immediately, use the SQL-auth string below.
> - For a **user-assigned** identity, use the same `ResourceId=` string and set the
>   identity on the data source (Advanced options / `identity` property).
> - **Prefer plain SQL auth?** Use the ordinary
>   `Server=tcp:contoso-estimator-sql.database.windows.net,1433;Database=contoso-rates;User ID=…;Password=…;`
>   form instead — then pieces 1 and 3 don't apply (Search connects directly).
> - **This choice only affects ingestion** (how Search reads rows). It has **no
>   impact on Track 2 RLS** — Track 2 enforces access at query time from the
>   end-user's Entra token against the index permission filter, independent of the
>   ingestion credential.

---

## 2. Azure AI Search with managed identity - aready created in previous module

```powershell
az search service create `
  --name $SEARCH --resource-group $RG --location $LOCATION `
  --sku basic --identity-type SystemAssigned

# Capture the search service managed identity principal id
$SEARCH_MI = az search service show --name $SEARCH --resource-group $RG `
  --query "identity.principalId" -o tsv
```

---

## 3. Models (from Module 1)

Confirm both deployments exist in the Foundry project:

| Model | Deployment name | Used by |
|-------|-----------------|---------|
| `text-embedding-3-small` | `text-embedding-3-small` | Vectorizing SQL rows (embedding columns) |
| `gpt-5-4` | `gpt-5-4` | Query planning + answer synthesis |

---

## 4. RBAC role assignments

| Identity | Role | Scope | Why |
|----------|------|-------|-----|
| **You** (signed-in user) | Search Service Contributor | Search service | Create knowledge sources / knowledge bases |
| **You** | Search Index Data Contributor | Search service | Load the generated index |
| **You** | Search Index Data Reader | Search service | Run retrieve queries |
| **Search MI** | Cognitive Services User | Foundry resource | Call the embedding model during ingestion |
| **Foundry project MI** | Search Index Data Reader | Search service | Agent MCP `knowledge_base_retrieve` |
| **App registration SP** | Search Index Data Reader | Search service | Consuming apps call retrieve |

```powershell
$SEARCH_ID  = az search service show --name $SEARCH --resource-group $RG --query id -o tsv
$FOUNDRY_ID = az cognitiveservices account show --name $FOUNDRY_ACCT --resource-group $RG --query id -o tsv
$ME         = az ad signed-in-user show --query id -o tsv

# You
az role assignment create --assignee $ME --role "Search Service Contributor"   --scope $SEARCH_ID
az role assignment create --assignee $ME --role "Search Index Data Contributor" --scope $SEARCH_ID
az role assignment create --assignee $ME --role "Search Index Data Reader"       --scope $SEARCH_ID

# Search MI -> Foundry (embeddings)
az role assignment create --assignee $SEARCH_MI --role "Cognitive Services User" --scope $FOUNDRY_ID

# Foundry project MI -> Search (fill in the project's managed identity principal id)
az role assignment create --assignee <foundry-project-mi-principal-id> `
  --role "Search Index Data Reader" --scope $SEARCH_ID
```

> Role changes can take a few minutes to propagate.

---

## 5. Entra security groups + test users (personas)

The RLS tracks use **region-based** personas by default. Create one group per
regional estimating office and add a test user to each.

```powershell
# --- Groups (mailNickname must be unique in the tenant) ---
$NSW = az ad group create --display-name "Estimating - NSW" --mail-nickname "grp-estimating-nsw" --query id -o tsv
$VIC = az ad group create --display-name "Estimating - VIC" --mail-nickname "grp-estimating-vic" --query id -o tsv

# --- Create two demo users (one per office) ---
# Use your verified tenant domain. Prompt for the password so it is never written to disk.
$DOMAIN = "MngEnvMCAP514681.onmicrosoft.com"
$DemoPwd = Read-Host "Demo user password (min 8 chars, meets tenant policy)" -AsSecureString
$DemoPwdPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
  [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($DemoPwd))

$NSW_USER = az ad user create `
  --display-name "NSW Estimator" `
  --user-principal-name "nsw-estimator@$DOMAIN" `
  --password $DemoPwdPlain `
  --force-change-password-next-sign-in false `
  --query id -o tsv

$VIC_USER = az ad user create `
  --display-name "VIC Estimator" `
  --user-principal-name "vic-estimator@$DOMAIN" `
  --password $DemoPwdPlain `
  --force-change-password-next-sign-in false `
  --query id -o tsv

# --- Add each user to their office group ---
az ad group member add --group $NSW --member-id $NSW_USER
az ad group member add --group $VIC --member-id $VIC_USER

# (Optional) tidy up the plaintext password variable
$DemoPwdPlain = $null
```

> **Already have users?** Skip the create step and resolve their object ids
> instead:
> ```powershell
> $NSW_USER = az ad user show --id "nsw-estimator@$DOMAIN" --query id -o tsv
> $VIC_USER = az ad user show --id "vic-estimator@$DOMAIN" --query id -o tsv
> ```
> **Security:** the password is captured via `Read-Host` so it never lands in a
> script or history. For real environments set
> `--force-change-password-next-sign-in true` and hand the temporary password to
> each user out-of-band.

> The `owner_group` column in `rate-library.sql` already tags every row with the
> matching group name (`grp-estimating-nsw`, `grp-estimating-vic`, …). Track 2
> maps those tags to the **group object IDs** above. Record the IDs — you will
> need them when building the permission-filtered index.
>
> **QLD/WA:** the seed data also tags `grp-estimating-qld` and `grp-estimating-wa`.
> Create those groups too if you want a four-office demo.

---

## 6. Entra app registration (consuming apps)

Both consuming apps sign in / acquire tokens through one app registration.

```powershell
$APP_ID = az ad app create --display-name "Contoso Estimator RLS Demo" `
  --sign-in-audience AzureADMyOrg `
  --web-redirect-uris "https://localhost:5001/signin-oidc" `
  --query appId -o tsv

# Service principal for the app (needed for role assignment)
az ad sp create --id $APP_ID

# Track 2 (delegated): allow the app to call Azure AI Search as the signed-in user.
# The Azure AI Search resource exposes a "user_impersonation" delegated scope.
# These IDs are well-known first-party values (same in every tenant):
#   --api            = Azure AI Search resource app id
#   user_impersonation scope id = az ad sp show --id "https://search.azure.com" `
#   --query "oauth2PermissionScopes[].{id:id,value:value}" -o json
az ad app permission add --id $APP_ID `
  --api "880da380-985e-4198-81b9-e05b1cc53158" `
  --api-permissions "a4165a31-5d9e-4120-bd1e-9d88c66fd3b8=Scope"

# Grant admin consent so the delegated permission is usable without per-user prompts
az ad app permission admin-consent --id $APP_ID
```

> **Verify the IDs in your tenant** (they should match the values above):
> ```powershell
> az ad sp show --id "https://search.azure.com" `
>   --query "{appId:appId, scopes:oauth2PermissionScopes[].{id:id,value:value}}" -o json
> ```
> The app then requests the scope `https://search.azure.com/user_impersonation`
> at runtime (Track 2 uses this to obtain a user token for Azure AI Search).

| App | Auth style | Needs |
|-----|-----------|-------|
| **Track 2** (`../src/track2-identity-rls`) — the shipped app | User sign-in (OIDC) + delegated Search token | Redirect URI + `user_impersonation` on `https://search.azure.com`, a client secret |

> **Track 1** is a [concept-only reference](03-foundry-iq-rls-security-filter.md)
> with no shipped code, so it needs no app registration here.

Create a client secret for the Track 2 web app:

```powershell
az ad app credential reset --id $APP_ID --display-name "rls-demo-secret" --query password -o tsv
```

> Store the client id / tenant id / secret in the app's `appsettings.json` or
> user-secrets — never commit them. Each app ships a `.env.example` /
> `appsettings.json` template.

---

## Setup verification checklist

| # | Check | How |
|---|-------|-----|
| 1 | `dbo.rate_library` has 156 rows | Re-run the verify query in the seed script |
| 2 | Search service is running + has a managed identity | `az search service show ... --query identity` |
| 3 | Embedding + chat models deployed | Foundry portal → Build → Models |
| 4 | Role assignments present | `az role assignment list --scope $SEARCH_ID -o table` |
| 5 | Two Entra groups exist with a member each | `az ad group member list --group $NSW -o table` |
| 6 | App registration has redirect URI + Search permission | Entra portal → App registrations |

Once every row is ✅, continue to the demo you want to run.

---

## References

| Resource | Link |
|----------|------|
| Azure SQL knowledge source (preview) | https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-azure-sql |
| Keyless (RBAC) auth for Search | https://learn.microsoft.com/azure/search/search-get-started-rbac |
| Managed identities in Azure AI Search | https://learn.microsoft.com/azure/search/search-how-to-managed-identities |
| Document-level access control | https://learn.microsoft.com/azure/search/search-document-level-access-overview |
| Azure SQL Database serverless | https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-overview |
