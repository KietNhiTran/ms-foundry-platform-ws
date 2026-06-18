# Module 7: Evaluation & Continuous Monitoring (45 min)

**Version:** 1.0  
**Last Updated:** June 2026  
**Format:** Led Demo  
**Prerequisite:** Module 2 complete (agent exists and can be queried)

---

## Objective

Run a batch evaluation against the Contoso Estimator agent using a labeled dataset, review built-in evaluators, and set up continuous monitoring for production traffic.

---

## Topics

### 7.1 Why Evaluate AI Agents?

"It works in the demo" ≠ "It works in production." Evaluation answers:
- Does the agent **ground** answers in the provided documents?
- Does the agent **use the right tools** for each query type?
- Does the agent **refuse** when it should (boundary testing)?
- How does the agent perform on **complex multi-step** queries?

### 7.2 Evaluation Types

| Type | When | How |
|------|------|-----|
| **Batch (offline)** | Before deployment, after changes | Run eval dataset → get scores |
| **Continuous (online)** | In production | Auto-sample live traffic → evaluate |
| **CI/CD gate** | On PR/deploy | Automated evaluation → pass/fail |

### 7.3 Built-in Evaluators

| Evaluator | What It Measures | Score Range |
|-----------|-----------------|:-:|
| **Task adherence** | Did the agent follow its instructions? | 1-5 |
| **Intent resolution** | Did it correctly identify what the user wanted? | 1-5 |
| **Tool call accuracy** | Did it call the right tools with correct params? | 0-1 |
| **Groundedness** | Is the response grounded in retrieved data? | 1-5 |
| **Relevance** | Is the response relevant to the question? | 1-5 |
| **Coherence** | Is the response well-structured and clear? | 1-5 |
| **Fluency** | Is the language natural and readable? | 1-5 |

### 7.4 LLM-as-Judge Pattern

Foundry uses an LLM (typically GPT-4.1) to evaluate agent responses. The judge LLM:
1. Receives the user query, agent response, and ground truth (if available)
2. Scores based on rubric criteria
3. Provides reasoning for the score

---

## Demo: Run Batch Evaluation

### Demo Steps

**Step 1: Review Evaluation Dataset**

Show the `contoso-estimator-eval.jsonl` file — labeled Q&A pairs:

```jsonl
{"query": "What is the concrete supply and pour rate for 32MPa in NSW?", "expected_answer": "$285.00/m³", "category": "rate_lookup"}
{"query": "What is the approval threshold for tenders between $20M and $50M?", "expected_answer": "Chief Estimating Officer with full estimate review panel", "category": "policy"}
{"query": "Calculate cost for 5000m³ of 40MPa concrete in VIC", "expected_answer": "$1,525,000 (5000 × $305.00)", "category": "calculation"}
```

**Step 2: Create Evaluation in Portal**

1. Navigate to **Build** → **Evaluations**
2. Click **Create evaluation**
3. Configure:
   - Name: `estimator-eval-v1`
   - Agent: `contoso-estimator-advisor`
   - Dataset: upload `contoso-estimator-eval.jsonl`
   - Evaluators: Groundedness, Relevance, Tool Call Accuracy
4. Run evaluation

**Step 3: Review Results**

- Overall scores per evaluator
- Per-query breakdown (which queries scored low?)
- Identify patterns: "rate lookup" queries score well, "multi-step calculations" need improvement

**Step 4: Set Up Continuous Monitoring**

1. Navigate to **Operate** → **Monitor** → **Continuous evaluation**
2. Configure:
   - Sampling rate: 10% of production queries
   - Evaluators: Groundedness + Relevance
   - Alert threshold: score drops below 3.5

---

## Sample Evaluation Dataset

See `data/contoso-estimator-eval.jsonl` for the full dataset covering:
- Rate lookups (10 queries)
- Policy questions (8 queries)
- Calculations (6 queries)
- Boundary tests (4 queries)
- Cross-document queries (4 queries)

---

## Key Takeaways

1. **Batch evaluation** catches issues before production deployment
2. **Built-in evaluators** cover the most common quality dimensions
3. **Continuous monitoring** auto-samples live traffic for ongoing quality assurance
4. Evaluation is **LLM-as-judge** — fast, scalable, no manual labeling needed for scoring
5. Integrate into **CI/CD** for automated deployment gates

---

## References

| Resource | Link |
|----------|------|
| Evaluate Agentic Workflows | https://learn.microsoft.com/azure/foundry/observability/how-to/evaluate-agent |
| Built-in Evaluators | https://learn.microsoft.com/azure/foundry/observability/reference/evaluator-library |
| Continuous Evaluation | https://learn.microsoft.com/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard#set-up-continuous-evaluation |
