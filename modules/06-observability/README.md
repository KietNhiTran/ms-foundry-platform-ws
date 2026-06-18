# Module 6: Observability & Tracing (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 2 complete (agent exists and has been queried)

---

## Objective

Connect Application Insights to the Contoso Estimator agent, explore traces showing tool calls and token usage, and use the Agent Monitoring Dashboard for fleet-level visibility.

---

## Topics

### 6.1 Why Observability for AI Agents?

Unlike traditional APIs where you monitor latency and errors, AI agents need visibility into:
- **Which tools** were called and in what order
- **Token consumption** per query (cost driver)
- **Retrieval quality** — did File Search return relevant docs?
- **Latency breakdown** — where is time spent (LLM vs tool vs network)?
- **Safety blocks** — how often are guardrails triggered?

### 6.2 Tracing Architecture

```
Agent Query → Application Insights
  │
  ├── Span: Agent invocation (total latency, tokens)
  │     ├── Span: LLM reasoning (model call, prompt tokens, completion tokens)
  │     ├── Span: Tool call — File Search (query, results count, latency)
  │     ├── Span: Tool call — Code Interpreter (code, execution time)
  │     └── Span: LLM synthesis (final response generation)
  │
  └── Custom attributes: agent_name, user_id, query_category
```

### 6.3 Two Levels of Tracing

| Level | What | Setup Effort | Detail |
|-------|------|:---:|------|
| **Server-side** | Automatic traces for all agent interactions | Zero (built-in) | Tool calls, tokens, latency |
| **Client-side** | Custom spans from your application code | Add SDK package | Correlation IDs, user context, business metrics |

### 6.4 Key Metrics

| Metric | What It Measures | Alert Threshold |
|--------|-----------------|:-:|
| `agent_run_duration_ms` | End-to-end query time | > 30s |
| `agent_run_success_rate` | % queries that complete successfully | < 95% |
| `token_usage_total` | Tokens consumed per query | Budget threshold |
| `content_filter_blocks` | Blocked requests count | Sudden spike |
| `tool_call_errors` | Failed tool executions | > 5% |
| `tool_call_latency_ms` | Individual tool response time | > 10s |

---

## Demo: Connect App Insights & Explore Traces

### Pre-Demo Setup Checklist

| # | Task | How | Verify |
|---|------|-----|--------|
| 1 | Application Insights resource exists | Azure Portal → Create App Insights | Resource running |
| 2 | Connection to Foundry project | Foundry portal → Settings → Tracing | Connected |
| 3 | Agent has been queried (Module 2) | Send test queries | Traces appear |

### Demo Steps

**Step 1: Connect Application Insights**

1. In Foundry portal → Project Settings → **Tracing**
2. Click **Connect** → select or create Application Insights resource
3. Enable **server-side tracing** (default ON)

**Step 2: Generate Traces**

Send several queries to the Contoso Estimator agent:
```
"What is the concrete rate in NSW?"
"Calculate total cost for 2000m³ concrete at QLD rates"
"Compare earthworks rates between NSW and QLD"
```

**Step 3: View Traces in Foundry Portal**

1. Navigate to **Operate** → **Tracing**
2. Select a recent trace → view the span tree:
   - Total duration
   - Token usage (prompt + completion)
   - Tool calls with individual latencies
   - Final response text

**Step 4: Explore Agent Monitoring Dashboard**

1. Navigate to **Operate** → **Monitor**
2. Show fleet-level metrics:
   - Active agents
   - Completion rate
   - Average latency
   - Token trends
   - Error rates

**Step 5: Sample KQL Query**

Show a custom KQL query in Log Analytics:

```kql
// Agent latency percentiles by query type (last 24h)
customMetrics
| where timestamp > ago(24h)
| where name == "agent_run_duration_ms"
| extend agentName = tostring(customDimensions["agent_name"])
| summarize 
    p50 = percentile(value, 50),
    p95 = percentile(value, 95),
    p99 = percentile(value, 99),
    count = count()
  by agentName
| order by p95 desc
```

---

## Key Takeaways

1. **Server-side tracing** is automatic — zero code changes required
2. Traces show the full **span tree** (LLM + tool calls + synthesis)
3. **Token usage** is the primary cost metric to monitor
4. **Agent Monitoring Dashboard** gives fleet-level visibility across all agents
5. **KQL** enables custom queries for deeper analysis

---

## References

| Resource | Link |
|----------|------|
| Trace Your Application | https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-setup |
| Monitor Agents Dashboard | https://learn.microsoft.com/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard |
| Application Insights | https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview |
| KQL Reference | https://learn.microsoft.com/kusto/query |
