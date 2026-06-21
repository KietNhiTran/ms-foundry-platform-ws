# KQL Queries for CIMIC Agent Observability

Paste these into **Application Insights > Logs** to monitor agent health.
Each query is standalone — copy the block and run it directly.

See Module 06, Section 4 for walkthrough.

---

## 1. Average latency per tool call (last 24 hours)

Shows which tools are slowest — useful for identifying bottlenecks
(e.g. Genie MCP vs File Search vs Code Interpreter)

```kql
dependencies
| where timestamp > ago(24h)
| where customDimensions has "gen_ai"
| summarize
    avg_duration_ms = avg(duration),
    p95_duration_ms = percentile(duration, 95),
    call_count = count()
    by name
| order by avg_duration_ms desc
| render barchart with (title="Average Latency per Tool Call")
```

---

## 2. Token usage over time (last 7 days, hourly buckets)

Tracks token consumption trends for cost monitoring

```kql
customMetrics
| where timestamp > ago(7d)
| where name in ("gen_ai.client.token.usage", "gen_ai.server.token.usage")
| extend token_type = tostring(customDimensions["gen_ai.token.type"])
| summarize total_tokens = sum(value) by bin(timestamp, 1h), token_type
| render timechart with (title="Token Usage Over Time")
```

---

## 3. Agent error rate timeline (last 7 days)

Spikes indicate agent failures — correlate with deployments or config changes.
GenAI spans land in the `dependencies` table (outgoing calls), not `requests`.

```kql
dependencies
| where timestamp > ago(7d)
| where customDimensions has "gen_ai"
| summarize
    total = count(),
    errors = countif(success == false),
    error_rate = round(100.0 * countif(success == false) / count(), 2)
    by bin(timestamp, 1h)
| render timechart with (title="Agent Error Rate (%)")
```

---

## 4. Cost attribution per agent (last 30 days)

Breakdown of token usage by agent for cost allocation.
Agent name and token counts are span-level attributes (`gen_ai.agent.name`,
`gen_ai.usage.input_tokens`, `gen_ai.usage.output_tokens`) on the
`dependencies` table — they are NOT dimensions on `customMetrics`.

```kql
dependencies
| where timestamp > ago(30d)
| extend agent_name = tostring(customDimensions["gen_ai.agent.name"])
| extend model = tostring(customDimensions["gen_ai.response.model"])
| extend input_tokens = toint(customDimensions["gen_ai.usage.input_tokens"])
| extend output_tokens = toint(customDimensions["gen_ai.usage.output_tokens"])
| where isnotempty(agent_name)
| summarize
    total_input = sum(input_tokens),
    total_output = sum(output_tokens),
    total_tokens = sum(input_tokens) + sum(output_tokens)
    by agent_name, model
| order by total_tokens desc
| render piechart with (title="Token Usage by Agent")
```

---

## 5. Top 10 slowest agent runs (last 24 hours)

Drill into individual slow runs to understand why.
GenAI spans land in the `dependencies` table (outgoing calls), not `requests`.

```kql
dependencies
| where timestamp > ago(24h)
| where customDimensions has "gen_ai"
| top 10 by duration desc
| project
    timestamp,
    operation_Id,
    name,
    duration_s = round(duration / 1000, 2),
    resultCode,
    success
```

---

## 6. Trace breakdown for a specific run (replace TRACE_ID)

Use this after finding a trace ID from the portal or query 5

```kql
union requests, dependencies, traces
| where operation_Id == "REPLACE_WITH_TRACE_ID"
| order by timestamp asc
| project timestamp, itemType, name, duration, message
```
