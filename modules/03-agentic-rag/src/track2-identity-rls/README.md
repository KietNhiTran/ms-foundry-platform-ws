# Track 2 — Identity-Native RLS Demo (.NET web, preview)

Restricts rate rows by the **signed-in user's Microsoft Entra identity**. The app
passes the user token to Azure AI Search, which trims rows using permission-filter
metadata — no filter logic in the app.

See the walkthrough: [../../docs/04-foundry-iq-rls-identity.md](../../docs/04-foundry-iq-rls-identity.md).

> ⚠️ **Preview** — uses `Azure.Search.Documents` `12.1.0-beta.1` and the
> `2026-05-01-preview` capabilities. Not for production.

## Prerequisites

- [Demo resources](../../docs/01-foundry-iq-azure-sql-setup.md) provisioned, including
  the Entra **groups**, **test users**, and **app registration** (with redirect URI
  `https://localhost:5001/signin-oidc` and delegated `user_impersonation` on
  `https://search.azure.com`).
- `dbo.rate_library` seeded in Azure SQL.

## Configure

Set values in [`appsettings.json`](appsettings.json) or user-secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "<client-secret>"
dotnet user-secrets set "Groups:grp-estimating-nsw" "<NSW-group-object-id>"
dotnet user-secrets set "Groups:grp-estimating-vic" "<VIC-group-object-id>"
```

| Key | Value |
|-----|-------|
| `AzureAd:TenantId` / `AzureAd:ClientId` / `AzureAd:ClientSecret` | App registration |
| `Search:Endpoint` | `https://<your-search>.search.windows.net` |
| `Search:KnowledgeBaseName` | `contoso-estimator-kb-secured` |
| `Search:SecuredIndexName` | `rate-library-secured` |
| `Sql:ConnectionString` | Azure SQL (Entra auth) |
| `Groups:*` | `owner_group` → Entra group object id |

## Step 1 — Seed the permission-filtered index (one-time)

```powershell
dotnet run -- --seed-index
```

This creates the `rate-library-secured` index (permission filtering **enabled**,
`group_ids` field marked as a **GroupIds** permission filter) and pushes every
rate row tagged with the Entra group id that matches its `owner_group`.

Then wrap that index as a `searchIndex` knowledge source
(`ks-rate-library-secured`) and add it to the knowledge base
`contoso-estimator-kb-secured`.

## Step 2 — Run the web app

```powershell
dotnet run
# browse https://localhost:5001
```

Sign in as the **NSW** test user → only NSW rates. Sign out, sign in as the
**VIC** test user → only VIC rates. Same code, same query — only the identity
differs.

## How it enforces access

- Sign-in via **Microsoft.Identity.Web** (OpenID Connect).
- The app acquires a token for `https://search.azure.com/user_impersonation`.
- It calls `RetrieveAsync(request, querySourceAuthorization: userToken)`.
- Azure AI Search compares the user's Entra group claims to the `group_ids`
  permission-filter field and returns only authorized rows.
