# Module 8: AI Gateway & Token Governance (40 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 1 complete (Foundry resource exists)

---

## Objective

Enable AI Gateway (APIM) for the Contoso Estimator agent to enforce token rate limits, cost controls per project team, and semantic caching for repeated estimation queries.

---

## Topics

### 8.1 AI Gateway Architecture

```
Clients (Estimators)
      │
      ▼
┌─────────────────────────────────────────┐
│         Azure API Management            │
│              (AI Gateway)               │
│                                         │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │ Rate Limit  │  │ Semantic Cache   │  │
│  │ (TPM/RPM)   │  │ (avoid repeats)  │  │
│  └─────────────┘  └─────────────────┘  │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │ Content     │  │ Usage Analytics  │  │
│  │ Safety      │  │ & Chargeback     │  │
│  └─────────────┘  └─────────────────┘  │
└─────────────────────────────────────────┘
      │
      ▼
Foundry Resource (GPT-5.4 deployment)
```

### 8.2 Key Governance Controls

| Control | What It Does | Business Value |
|---------|-------------|----------------|
| **TPM limit** | Cap tokens per minute per subscription | Prevent runaway costs |
| **Daily quota** | Max tokens per day per team | Budget allocation |
| **Semantic caching** | Return cached response for semantically similar queries | 30-50% cost reduction for repeated patterns |
| **Content safety at gateway** | Apply safety policies before model | Defense in depth |
| **Usage analytics** | Track tokens by team/project/user | Cost allocation & chargeback |
| **Load balancing** | Distribute across multiple deployments | High availability |

### 8.3 Cost Management for Construction Estimation

| Team | Monthly TPM Budget | Rationale |
|------|:-:|------|
| Estimating (active tenders) | 500K | Heavy usage during tender preparation |
| Estimating (BAU) | 100K | Occasional rate lookups |
| Project managers | 200K | Ad-hoc project queries |
| Executives | 50K | Portfolio summaries |

---

## Demo: Associate AI Gateway

### Demo Steps

**Step 1: View AI Gateway in Foundry**

1. Navigate to **Operate** → **AI Gateway**
2. Show the APIM association (or associate a new APIM instance)
3. View current usage metrics

**Step 2: Configure Rate Limits**

Show the APIM policy for token rate limiting:

```xml
<policies>
    <inbound>
        <!-- Rate limit by subscription key (per team) -->
        <rate-limit-by-key
            calls="100"
            renewal-period="60"
            counter-key="@(context.Subscription.Id)" />
        
        <!-- Token quota per day -->
        <quota-by-key
            calls="10000"
            renewal-period="86400"
            counter-key="@(context.Subscription.Id)" />
        
        <!-- Semantic caching for repeated queries -->
        <azure-openai-semantic-cache-lookup
            score-threshold="0.9"
            embeddings-backend-id="text-embedding-backend" />
    </inbound>
    <outbound>
        <azure-openai-semantic-cache-store duration="3600" />
    </outbound>
</policies>
```

**Step 3: Demonstrate Rate Limiting**

1. Send rapid queries to trigger rate limit
2. Show the 429 response with retry-after header
3. Show how quota resets

**Step 4: Show Usage Analytics**

1. View token consumption by subscription/team
2. Show cost allocation dashboard
3. Discuss chargeback model for construction project teams

---

## Key Takeaways

1. **AI Gateway** (APIM) sits between clients and the Foundry model deployment
2. **TPM/RPM limits** prevent runaway costs — set per team/project
3. **Semantic caching** reduces costs 30-50% for repeated estimation patterns
4. **Usage analytics** enable cost allocation and chargeback across project teams
5. Gateway adds **zero latency** for non-cached requests (< 5ms overhead)

---

## References

| Resource | Link |
|----------|------|
| AI Gateway | https://learn.microsoft.com/azure/foundry/control-plane/how-to/ai-gateway |
| Plan and Manage Costs | https://learn.microsoft.com/azure/foundry/concepts/manage-costs |
| APIM + AI | https://learn.microsoft.com/azure/api-management/api-management-ai-overview |
| Semantic Caching | https://learn.microsoft.com/azure/api-management/azure-openai-semantic-cache-lookup-policy |
