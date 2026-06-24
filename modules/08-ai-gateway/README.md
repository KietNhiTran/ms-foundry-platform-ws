# Module 8: AI Gateway & Token Governance

**Version:** 1.1  
**Last Updated:** June 2026  
**Duration:** 40 minutes (Presenter-Led Demo)  
**Objective:** Enable the AI Gateway (Azure API Management) for your Foundry resource, configure token rate limiting and quotas, govern MCP tool access, and understand semantic caching and content safety policies.

> **Prerequisites:** Modules 1–7 completed. An Azure API Management instance is available (or will be created during the demo).

> **Deep-dive labs:** [AI Gateway Labs - Azure APIM ❤️ Microsoft Foundry](https://azure-samples.github.io/AI-Gateway/) — hands-on notebooks covering advanced scenarios (load balancing, circuit breaking, prompt flow integration, and more).

### Pre-Demo Setup Checklist

Complete these steps **before** the demo session:

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | **Azure API Management instance** exists | Azure portal → Create resource → API Management (Developer tier). **APIM creation takes 20–40 min** — do this well in advance | APIM resource shows "Active" in portal |
| 2 | **Associate APIM with Foundry** | Foundry portal → Operate → Admin → AI Gateway → select your APIM instance | AI Gateway section shows the associated APIM |
| 3 | **Set low TPM limit for demo** | Operate → Admin → AI Gateway → Token Management → + Set limit → gpt-4o → **10,000 TPM** | Limit appears in Token Management list |
| 4 | **Install Python dependencies** | `cd modules/08-ai-gateway/src && pip install -r requirements.txt` | `python -c "import azure.ai.projects"` succeeds |
| 5 | **Configure .env** | `cp .env.example .env` — fill in `PROJECT_ENDPOINT` | `.env` has non-empty `PROJECT_ENDPOINT` |
| 6 | **Presenter logged in** | `az login` with Foundry User + API Management Service Contributor roles | `az account show` shows correct subscription |

> **Post-demo cleanup:** Reset the TPM limit back to production levels (or remove it) after the demo to avoid throttling real traffic.

---

## 8.1 Enable AI Gateway *(Portal — 10 min)*

The AI Gateway sits between clients and model deployments, providing a single chokepoint for rate limiting, cost control, content safety, and governance.

### Architecture


![AI Gateway Architecture — APIM sits between clients and Foundry resources (agents, models, MCP/A2A tools), enforcing policies, limits, and controls](gateway-architecture-diagram.png)

### Steps

1. **Operate** → **Admin** → **AI Gateway**
2. Either:
   - **Associate existing APIM**: Select your API Management instance
   - **Create new APIM**: Foundry creates one automatically
3. Once associated, all model requests flow through the gateway

### Verify the Association

After enabling the gateway, confirm it is actually wired to your Foundry project and model deployments:

| # | Check | Where | What to Look For |
|---|-------|-------|------------------|
| 1 | **Gateway status** | Foundry → Operate → Admin → AI Gateway | Status shows **Active** and lists your APIM instance name |
| 2 | **Deployments routed** | Same page → scroll to **Deployments** section | Your model deployments (e.g., `gpt-4o`) appear with a ✓ indicating they are routed through the gateway |
| 3 | **APIM has the API** | Azure portal → your APIM instance → **APIs** | An API named like `azure-ai-service-api` or `Azure OpenAI Service API` exists (auto-created by the association) |
| 4 | **Token management available** | Foundry → Operate → Admin → AI Gateway → **Token Management** | You can add TPM limits — this section is only visible when the gateway is active |

**Quick CLI check** — confirm APIM has the Foundry-generated API:

```bash
az apim api list \
  --resource-group <your-rg> \
  --service-name <your-apim-name> \
  --query "[].{name:name, displayName:displayName, path:path}" \
  -o table
```

You should see an API entry with a path like `openai` or `azure-openai`. If the list is empty, the association did not complete — go back to the Foundry portal and re-associate.

**Quick traffic check** — send a single request and verify it flows through APIM:

1. Run a simple agent or model query (e.g., from the Foundry playground)
2. In Azure portal → your APIM instance → **Monitor** → **Metrics**
3. Select metric **Requests** with a 5-minute time range
4. You should see at least 1 request — this confirms traffic is flowing through the gateway

> **Troubleshooting:** If the gateway shows as associated but no traffic appears in APIM metrics, check that the model deployment was created **after** the gateway was enabled. Pre-existing deployments may need to be re-deployed to pick up the gateway routing.

### Verify Gateway Traffic (KQL Queries)

After running tests (playground, scripts, or agent chat), verify traffic in APIM logs. Go to **Azure portal → Log Analytics workspace → Logs** and run these queries:

**All recent gateway traffic (model + MCP):**

```kusto
ApiManagementGatewayLogs
| where TimeGenerated > ago(1h)
| project TimeGenerated, ResponseCode, ApiId, BackendUrl, BackendResponseCode
| order by TimeGenerated desc
| take 30
```

**MCP tool traffic only:**

```kusto
ApiManagementGatewayLogs
| where TimeGenerated > ago(1h)
| where ApiId contains "tool" or BackendUrl contains "mcp"
| project TimeGenerated, ResponseCode, ApiId, BackendUrl, BackendResponseCode
| order by TimeGenerated desc
| take 20
```

**Traffic breakdown by API (model vs MCP):**

```kusto
ApiManagementGatewayLogs
| where TimeGenerated > ago(2h)
| summarize RequestCount=count(),
            SuccessCount=countif(ResponseCode < 400),
            FailedCount=countif(ResponseCode >= 400)
    by ApiId
| order by RequestCount desc
```

**Errors only (for debugging):**

```kusto
ApiManagementGatewayLogs
| where TimeGenerated > ago(1h)
| where ResponseCode >= 400
| project TimeGenerated, ResponseCode, ApiId, LastErrorSection, LastErrorSource, LastErrorMessage
| order by TimeGenerated desc
| take 20
```

> **Note:** Diagnostic settings must be enabled on the APIM instance for logs to appear (Azure portal → APIM → Diagnostic settings → enable **GatewayLogs** → send to Log Analytics workspace). Logs take **5–10 minutes** to ingest after requests are made.

### What the Gateway Provides

| Capability | Description |
|------------|-------------|
| **Token rate limiting** | TPM (tokens per minute) caps per project |
| **Token quotas** | Daily/weekly/monthly total token budgets |
| **MCP tool governance** | Rate limiting and IP filtering for MCP servers |
| **Content safety** | Pre-screen prompts via Azure AI Content Safety |
| **Semantic caching** | Cache similar prompt responses (requires Redis) |
| **Token metrics** | Emit usage metrics to Application Insights |

---

## 8.2 Token Rate Limiting *(Portal + Script — 10 min)*

### Configure Limits

1. **Operate** → **Admin** → **AI Gateway** → **Token Management**
2. Click **+ Set limit**
3. Select project and model deployment (e.g., `gpt-4o`)
4. Set **Limit (Token-per-minute)**: `10000` (low for demo purposes)
5. Click **Create**

### Demo: Trigger Rate Limiting

```bash
cd modules/08-ai-gateway/src
pip install -r requirements.txt
cp .env.example .env   # Fill in PROJECT_ENDPOINT
python test_rate_limit.py
```

The script sends 15 rapid-fire requests. With a 10K TPM limit, you should see:
- First several requests: `✓ OK`
- Later requests: `⛔ 429 Rate Limited`

### Rate Limit Headers

When rate-limited, the response includes:
- `X-RateLimit-Remaining`: Tokens remaining in the current window
- `Retry-After`: Seconds until the limit resets

> **Reset the limit** after the demo: increase TPM back to production levels or remove the limit entirely.

---

## 8.3 Applying APIM Policies *(Portal — 10 min)*

The policy snippets in [`apim_policies.xml`](apim_policies.xml) are **individual building blocks** — you combine the ones you need into a single policy document on the API.

### How to Apply Policies

1. Azure portal → your APIM instance → **APIs**
2. Click the API (e.g., `your-foundry-resource`)
3. Click **All operations**
4. In the **Inbound processing** section, click the **`</>`** code editor
5. You'll see the base policy structure:

```xml
<policies>
    <inbound>
        <base />
        <set-backend-service backend-id="your-foundry-resource" />
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```

6. **Add policy elements from `apim_policies.xml`** into the appropriate section:
   - **Inbound policies** (rate limiting, content safety, IP filtering) → add after `<set-backend-service .../>` inside `<inbound>`
   - **Outbound policies** (token metrics, cache store) → add after `<base />` inside `<outbound>`
7. Click **Save**

### Example: Combined Policy

```xml
<policies>
    <inbound>
        <base />
        <!-- Required: route to Foundry backend -->
        <set-backend-service backend-id="your-foundry-resource" />

        <!-- Policy 1: Token Rate Limiting (10K TPM per subscription) -->
        <llm-token-limit
            tokens-per-minute="10000"
            counter-key="@(context.Subscription.Id)"
            estimate-prompt-tokens="true"
            remaining-tokens-variable-name="remainingTokens" />

        <!-- Policy 5: Content Safety (requires Content Safety backend) -->
        <llm-content-safety backend-id="content-safety-backend">
            <categories>
                <category name="Hate" threshold="4" />
                <category name="Violence" threshold="4" />
                <category name="SelfHarm" threshold="4" />
                <category name="Sexual" threshold="4" />
            </categories>
        </llm-content-safety>

        <!-- Policy 4: Semantic Cache Lookup (requires Azure Managed Redis) -->
        <llm-semantic-cache-lookup
            score-threshold="0.8"
            embeddings-backend-id="embeddings-deployment"
            embeddings-backend-auth="system-assigned" />

        <!-- Policy 7: IP Filtering (restrict to corporate network) -->
        <!--
        <ip-filter action="allow">
            <address-range from="10.0.0.0" to="10.255.255.255" />
            <address-range from="172.16.0.0" to="172.31.255.255" />
        </ip-filter>
        -->
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />

        <!-- Policy 3: Token Usage Metrics (emit to Application Insights) -->
        <llm-emit-token-metric namespace="ContosoAIGateway">
            <dimension name="Subscription" value="@(context.Subscription.Name)" />
            <dimension name="Model" value="@(context.Request.Headers.GetValueOrDefault(&quot;model&quot;,&quot;unknown&quot;))" />
            <dimension name="Agent" value="@(context.Request.Headers.GetValueOrDefault(&quot;x-agent-name&quot;,&quot;unknown&quot;))" />
        </llm-emit-token-metric>

        <!-- Policy 4: Semantic Cache Store (pair with lookup above) -->
        <llm-semantic-cache-store duration="3600" />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```

> **For the demo:** Start with only `<set-backend-service>` and policy 1 (token rate limiting). The content safety, semantic caching, and IP filtering policies require additional Azure resources — uncomment or add them when those resources are provisioned.

> **Important:** The `<set-backend-service>` line is required — without it, APIM returns `500 Backend service URL is not defined`. Keep it as the first element after `<base />` in inbound.

### Policy Reference — Where to Add & How to Test

Each policy below is a standalone snippet from [`apim_policies.xml`](apim_policies.xml). Add them to the correct API and section in APIM.

---

#### Policy 1: Token Rate Limiting (`llm-token-limit`)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) |
| **Section** | `<inbound>` — after `<set-backend-service>` |
| **Prerequisites** | None — works out of the box |

```xml
<llm-token-limit
    tokens-per-minute="500"
    counter-key="@(context.Subscription.Id)"
    estimate-prompt-tokens="false"
    remaining-tokens-variable-name="remainingTokens" />
```

**Test:** Run `python test_rate_limit.py` (with `APIM_GATEWAY_URL` set in `.env`). Expect `⛔ 429` after ~4 requests at 500 TPM.

---

#### Policy 2: Token Quota (`llm-token-limit` with quota)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) |
| **Section** | `<inbound>` — after `<set-backend-service>` |
| **Prerequisites** | None |

```xml
<llm-token-limit
    tokens-per-minute="50000"
    token-quota="500000"
    token-quota-period="86400"
    counter-key="@(context.Subscription.Id)"
    estimate-prompt-tokens="false" />
```

**Test:** Send requests until cumulative tokens exceed 500K. Expect `403 Forbidden` (quota exhausted) instead of `429` (rate limited). Reset by waiting for the quota period to expire.

---

#### Policy 3: Token Usage Metrics (`llm-emit-token-metric`)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) |
| **Section** | `<outbound>` — after `<base />` |
| **Prerequisites** | Application Insights connected to APIM (Azure portal → APIM → Application Insights → select your App Insights instance) |

```xml
<llm-emit-token-metric namespace="ContosoAIGateway">
    <dimension name="Subscription" value="@(context.Subscription.Name)" />
    <dimension name="Model" value="@(context.Request.Headers.GetValueOrDefault(&quot;model&quot;,&quot;unknown&quot;))" />
    <dimension name="Agent" value="@(context.Request.Headers.GetValueOrDefault(&quot;x-agent-name&quot;,&quot;unknown&quot;))" />
</llm-emit-token-metric>
```

**Test:**
1. Send several requests through the gateway
2. Application Insights → **Metrics** → Custom namespace: `ContosoAIGateway`
3. Verify token consumption metrics appear with Subscription, Model, and Agent dimensions
4. Allow 2–5 minutes for metrics to appear

---

#### Policy 4: Semantic Caching (`llm-semantic-cache-lookup` + `llm-semantic-cache-store`)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) |
| **Section** | `<inbound>` for lookup, `<outbound>` for store |
| **Prerequisites** | **Azure Managed Redis** configured as an [external cache](https://learn.microsoft.com/azure/api-management/api-management-howto-cache-external) in APIM, plus an **embeddings model deployment** (e.g., `text-embedding-ada-002`) registered as an APIM backend |

**Inbound (add after `<set-backend-service>`):**
```xml
<llm-semantic-cache-lookup
    score-threshold="0.8"
    embeddings-backend-id="embeddings-deployment"
    embeddings-backend-auth="system-assigned" />
```

**Outbound (add after `<base />`):**
```xml
<llm-semantic-cache-store duration="3600" />
```

**Test:**
1. Send a prompt: *"What is Contoso's estimation policy for civil works?"*
2. Note the response time (e.g., 3s)
3. Send the same or similar prompt again
4. Second response should be much faster (< 100ms) — served from cache
5. Verify in APIM Metrics: look for cache hit/miss counts

> **Note:** This policy is shown conceptually in this workshop — Azure Managed Redis is not provisioned.

---

#### Policy 5: Content Safety (`llm-content-safety`)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) |
| **Section** | `<inbound>` — after `<set-backend-service>` |
| **Prerequisites** | **Azure AI Content Safety** resource created and registered as an APIM [backend](https://learn.microsoft.com/azure/api-management/backends) with ID `content-safety-backend` |

```xml
<llm-content-safety backend-id="content-safety-backend">
    <categories>
        <category name="Hate" threshold="4" />
        <category name="Violence" threshold="4" />
        <category name="SelfHarm" threshold="4" />
        <category name="Sexual" threshold="4" />
    </categories>
</llm-content-safety>
```

**Test:**
1. Send a normal prompt: *"What is the estimated cost for a 500m bridge?"* → should return `200 OK`
2. Send a prompt that violates content filters → should return `400` with a content safety error **before** the prompt reaches the model
3. Check APIM logs for requests blocked by content safety

> **Note:** This provides defence in depth alongside Foundry Guardrails (Module 09). Content Safety blocks at the gateway; Guardrails block at the model.

---

#### Policy 6: MCP Rate Limiting (`rate-limit-by-key`)

| | Detail |
|---|---|
| **APIM API** | MCP Server API (find it under APIM → **MCP Servers** in the left sidebar) |
| **Section** | `<inbound>` — after existing `<cors>` block |
| **Prerequisites** | MCP tool must be governed by AI Gateway (the "Governed with AI Gateway" checkbox must be checked on the tool's Details page) |

```xml
<rate-limit-by-key calls="60" renewal-period="60"
    counter-key="@(context.Request.Headers.GetValueOrDefault(&quot;x-user-id&quot;,&quot;anonymous&quot;))" />
```

**Where to find the MCP API:**
1. APIM → **MCP Servers** (left sidebar) → click your MCP server
2. Or: APIM → **APIs** (may only be visible with API version `2025-09-01-preview`)
3. The MCP API is separate from the model API — each has its own policy scope

**Test:**
1. Use the agent to make MCP tool calls (e.g., trigger a tool query in the playground)
2. Send more than 60 requests in 1 minute
3. Expect `429 Too Many Requests` on the MCP tool call
4. Verify in APIM logs: filter by `ApiId contains "tool"`

---

#### Policy 7: IP Filtering (`ip-filter`)

| | Detail |
|---|---|
| **APIM API** | `your-foundry-resource` (model API) and/or MCP Server API |
| **Section** | `<inbound>` — after `<set-backend-service>` |
| **Prerequisites** | Know your corporate IP ranges |

```xml
<ip-filter action="allow">
    <address-range from="10.0.0.0" to="10.255.255.255" />
    <address-range from="172.16.0.0" to="172.31.255.255" />
    <!-- Add your corporate IP ranges here -->
</ip-filter>
```

**Test:**
1. Send a request from an allowed IP → should return `200 OK`
2. Send from a non-allowed IP (e.g., mobile hotspot) → should return `403 Forbidden`
3. Check APIM logs for blocked requests

> **Caution:** Don't enable this during the workshop unless you know your current IP is in the allowed range. You could lock yourself out.

> **For the demo:** Apply policy 1 (token rate limiting) only. This is what `test_rate_limit.py` tests. The other policies require additional Azure resources or configuration as noted in their prerequisites.

---

## 8.4 MCP Tool Governance *(Portal — 5 min)*

When the AI Gateway is enabled and an MCP tool is added with the **"Governed with AI Gateway"** checkbox active, Foundry creates an MCP Server entry in APIM. MCP tool calls then flow through APIM where policies (rate limiting, IP filtering, etc.) can be applied independently from model policies.

### Verify MCP Routing

1. APIM → **MCP Servers** (left sidebar) → confirm your MCP tool appears
2. Run an agent query that triggers the MCP tool
3. Check APIM logs (allow 5–10 min for log ingestion):

```kusto
ApiManagementGatewayLogs
| where TimeGenerated > ago(1h)
| where ApiId contains "tool"
| project TimeGenerated, ResponseCode, ApiId, BackendUrl
| order by TimeGenerated desc
| take 10
```

### Apply MCP-Specific Policies

MCP policies are applied on the **MCP Server API** in APIM (separate from the model API). See **Policy 6** and **Policy 7** above for details.

| Policy | Purpose | Where |
|--------|---------|-------|
| `rate-limit-by-key` | Limit MCP calls per user | MCP Server API → `<inbound>` |
| `ip-filter` | Restrict MCP access to corporate network | MCP Server API → `<inbound>` |

See [`apim_policies.xml`](apim_policies.xml) for policy snippets (sections 6 and 7).

---

## 8.5 Key Takeaways *(Wrap-up — 5 min)*

| Concept | What You Learned |
|---------|-----------------|
| **AI Gateway** | Single chokepoint for all AI traffic governance |
| **Token rate limiting** | TPM caps prevent runaway consumption |
| **Token quotas** | Daily/monthly budgets per project |
| **MCP governance** | Rate limit and IP-filter MCP tool calls |
| **Semantic caching** | Reuse responses for similar prompts (cost savings) |
| **Content safety** | Pre-screen prompts at the gateway level |
| **Zero code changes** | Agents don't need modification — governance is infrastructure |

---

## References

| Resource | Link |
|----------|------|
| AI Gateway Labs (hands-on) | [Azure APIM ❤️ Microsoft Foundry](https://azure-samples.github.io/AI-Gateway/) |
| AI Gateway | https://learn.microsoft.com/azure/foundry/control-plane/how-to/ai-gateway |
| Plan and Manage Costs | https://learn.microsoft.com/azure/foundry/concepts/manage-costs |
| APIM + AI | https://learn.microsoft.com/azure/api-management/api-management-ai-overview |
| Semantic Caching | https://learn.microsoft.com/azure/api-management/azure-openai-semantic-cache-lookup-policy |

---

## Files Reference

| File | Purpose |
|------|---------|
| [`test_rate_limit.py`](test_rate_limit.py) | Rate limit demonstration script |
| [`apim_policies.xml`](apim_policies.xml) | APIM policy snippets (7 policies) |
| [`requirements.txt`](requirements.txt) | Python dependencies |
