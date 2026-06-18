# Module 10: Fleet Management & Agent 365 (50 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** At least Module 2 complete (agent exists)

---

## Objective

Demonstrate Foundry Control Plane for managing agents at enterprise scale, and show how to publish the Contoso Estimator agent to Microsoft Teams for bid team collaboration.

---

## Topics

### 10.1 Foundry Control Plane

The **Control Plane** provides a single pane of glass for all AI assets across your organization:

```
┌──────────────────────────────────────────────────────────────┐
│                   Foundry Control Plane                       │
│                                                              │
│  ┌─────────────┐  ┌────────────────┐  ┌─────────────────┐  │
│  │ Agent Fleet │  │ Model Registry │  │ Cost & Quotas    │  │
│  │ Dashboard   │  │ & Deployments  │  │ Management       │  │
│  └─────────────┘  └────────────────┘  └─────────────────┘  │
│                                                              │
│  ┌─────────────┐  ┌────────────────┐  ┌─────────────────┐  │
│  │ Compliance  │  │ Agent Health   │  │ External Agent   │  │
│  │ & Safety    │  │ Scoring        │  │ Registration     │  │
│  └─────────────┘  └────────────────┘  └─────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 10.2 Fleet-Level KPIs

| KPI | What It Measures | Target |
|-----|-----------------|:---:|
| Active agents | Total agents in production | — |
| Run completion rate | % of queries that complete successfully | > 99% |
| Average latency | Mean response time across fleet | < 15s |
| Token cost trend | Daily/weekly token spend | Within budget |
| Safety blocks | Queries blocked by guardrails | < 2% |
| Health score | Composite agent health (latency + errors + safety) | > 85/100 |

### 10.3 Agent Lifecycle Management

```
Create (Dev) → Evaluate → Deploy (Staging) → Promote (Production) → Monitor → Update
     ↑                                                                    │
     └────────────────────── Version management ──────────────────────────┘
```

### 10.4 Publishing to Microsoft 365

| Channel | Description | Use Case |
|---------|-------------|----------|
| **Microsoft Teams** | Agent appears as a bot in Teams channels/chats | Team collaboration (bid teams) |
| **BizChat (Copilot)** | Agent available in Microsoft 365 Copilot | Personal productivity |
| **Azure Bot Service** | Custom web/mobile/voice channels | External-facing applications |

---

## Demo: Fleet Dashboard + Publish to Teams

### Demo Steps

**Step 1: Explore Fleet Dashboard**

1. Navigate to **Operate** → **Fleet** (or Control Plane)
2. Show:
   - All agents across the project
   - Health scores
   - Token usage trends
   - Error rates
   - Safety block counts

**Step 2: Agent Inventory**

1. View all registered agents:
   - `contoso-estimator-advisor` (our agent)
   - Show agent versions, last updated, deployment status
2. Discuss: registering external (non-Foundry) agents for unified management

**Step 3: Publish Agent to Teams**

1. Select `contoso-estimator-advisor` → **Publish**
2. Select **Microsoft Teams** channel
3. Configure:
   - Display name: "Contoso Estimator"
   - Description: "AI advisor for construction cost estimation"
   - Icon: construction-themed icon
4. Publish → agent appears as a bot in Teams

**Step 4: Test in Teams (if configured)**

1. Open Microsoft Teams
2. Search for "Contoso Estimator" bot
3. Send: "What's the concrete rate in NSW?"
4. Show: same agent, same responses, accessible in Teams

**Step 5: Discuss Enterprise Governance**

Talking points:
- **Multi-tenant:** different divisions can have their own agents, all visible in fleet dashboard
- **Version management:** roll back to previous agent version if issues arise
- **Compliance:** audit trail of all agent interactions across fleet
- **Cost allocation:** per-agent cost tracking for internal chargeback

---

## Key Takeaways

1. **Control Plane** gives organization-wide visibility across all AI agents
2. **Fleet dashboard** shows health, cost, safety, and usage trends
3. **Publishing to Teams** makes agents accessible where teams already work
4. **Agent versions** enable safe updates with rollback capability
5. **External agents** (non-Foundry) can be registered for unified management

---

## References

| Resource | Link |
|----------|------|
| Foundry Control Plane | https://learn.microsoft.com/azure/foundry/control-plane/overview |
| Publish to Copilot | https://learn.microsoft.com/azure/foundry/agents/how-to/publish-copilot |
| Agent Lifecycle | https://learn.microsoft.com/azure/foundry/agents/concepts/agent-lifecycle |
| Fleet Management | https://learn.microsoft.com/azure/foundry/control-plane/how-to/fleet-management |
