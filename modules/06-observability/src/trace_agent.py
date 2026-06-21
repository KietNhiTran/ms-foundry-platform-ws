"""
Contoso Estimator — Client-Side Tracing Demo

Instruments the Contoso Estimator Advisor agent with Azure Monitor
OpenTelemetry, sends 3 sample queries, and prints trace IDs so the
presenter can verify traces in the Foundry portal (Operate > Assets >
Agents > Traces).

Client-side tracing setup follows the official Foundry guide:
  https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-client-side

Key steps:
  1. Set AZURE_EXPERIMENTAL_ENABLE_GENAI_TRACING=true (BEFORE imports)
  2. Call AIProjectInstrumentor().instrument() to auto-instrument SDK calls
  3. configure_azure_monitor() to export traces to Application Insights
  4. Use agent_reference with both name and id in responses.create()

Usage:
    1. Copy .env.example -> .env and fill in values
    2. pip install -r requirements.txt
    3. python trace_agent.py

See Module 06 README for full context.
"""

import os
import time

# ── STEP 1: Enable GenAI tracing BEFORE any azure imports ────────────────
# This env var MUST be set before AIProjectInstrumentor is imported.
# Without it, tracing instrumentation is silently skipped.
os.environ["AZURE_EXPERIMENTAL_ENABLE_GENAI_TRACING"] = "true"

# Optional: capture message contents (user inputs, model outputs, tool args)
# in trace spans. Useful for debugging but may contain sensitive data.
# Disable in production. Default: false.
os.environ["OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT"] = "true"

from dotenv import load_dotenv

load_dotenv()

from azure.ai.projects import AIProjectClient
from azure.ai.projects.telemetry import AIProjectInstrumentor
from azure.identity import DefaultAzureCredential
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry import trace

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
PROJECT_ENDPOINT = os.environ["PROJECT_ENDPOINT"]
AGENT_NAME = os.environ.get("AGENT_NAME", "contoso-estimator-advisor")
AGENT_VERSION = os.environ.get("AGENT_VERSION", "")

# ---------------------------------------------------------------------------
# Connect to Foundry
# ---------------------------------------------------------------------------
credential = DefaultAzureCredential()
project = AIProjectClient(endpoint=PROJECT_ENDPOINT, credential=credential)

# ---------------------------------------------------------------------------
# STEP 2: Enable client-side instrumentation
# ---------------------------------------------------------------------------
# AIProjectInstrumentor auto-instruments all OpenAI Responses API and
# Conversations API calls made through clients obtained via get_openai_client().
# This creates GenAI-standard spans (gen_ai.client.*, gen_ai.server.*) that
# capture model calls, tool invocations, token usage, and latency.
#
# Parameters:
#   enable_content_recording: capture message text in spans (default: False)
#   enable_trace_context_propagation: inject W3C traceparent headers so
#       client-side and server-side spans are correlated (default: True)
AIProjectInstrumentor().instrument(
    enable_content_recording=True,          # Demo only — disable in production
    enable_trace_context_propagation=True,   # Correlate client + server spans
)

# ---------------------------------------------------------------------------
# STEP 3: Configure Azure Monitor as the trace exporter
# ---------------------------------------------------------------------------
# This sends all OpenTelemetry spans to Application Insights.
# The connection string is retrieved from the Foundry project (it knows
# which App Insights resource is connected).
app_insights_cs = project.telemetry.get_application_insights_connection_string()

if not app_insights_cs:
    print("[WARN] No Application Insights connected to this project.")
    print("  Go to Foundry portal > Operate > Admin > Connected Resources > Add AppInsights")
    raise SystemExit(1)

print("Configuring Azure Monitor tracing...")
print(f"  App Insights: ...{app_insights_cs[-30:]}")

configure_azure_monitor(connection_string=app_insights_cs)

# Create a tracer for our custom spans (wrapping each demo query)
tracer = trace.get_tracer("contoso.workshop.module06")

# ---------------------------------------------------------------------------
# Sample Contoso Estimator queries to generate traces
# ---------------------------------------------------------------------------
DEMO_QUERIES = [
    "What is the approval threshold for a $25M tender submission?",
    "Calculate a preliminary estimate for 500m³ of concrete at Sydney rates.",
    "What margin guidelines apply to projects over $50M?",
]


def run_agent_query(client: AIProjectClient, agent_name: str, query: str, version: str = "") -> str:
    """Send a single query to the Contoso Estimator agent and return the response text.

    Uses the Responses API with agent_reference to invoke a Foundry agent.
    The agent_reference must include both 'name' and 'id' for trace
    correlation in the Foundry portal.
    """
    # get_openai_client() MUST be called AFTER AIProjectInstrumentor().instrument()
    # so the client is auto-instrumented.
    openai = client.get_openai_client()

    # Resolve the agent — the Responses API requires the model parameter
    agent = client.agents.get(agent_name=agent_name)
    agent_model = agent.versions.latest.definition.model

    # Build agent reference — include both name and id so traces are
    # correlated to the correct agent in the Foundry portal Traces view.
    agent_ref = {
        "name": agent_name,
        "id": agent.id,
        "type": "agent_reference",
    }
    if version:
        agent_ref["version"] = version

    response = openai.responses.create(
        model=agent_model,
        input=query,
        store=True,
        extra_body={"agent_reference": agent_ref},
    )

    # Extract text from the response output items
    output_text = ""
    if response.output:
        for item in response.output:
            if hasattr(item, "content") and item.content:
                for block in item.content:
                    if hasattr(block, "text"):
                        output_text += block.text
    return output_text or "(No response text)"


def main():
    print(f"\n{'='*60}")
    print(f"  Contoso Estimator — Tracing Demo (Module 06)")
    print(f"  Agent: {AGENT_NAME}")
    print(f"{'='*60}\n")

    for i, query in enumerate(DEMO_QUERIES, 1):
        # Custom span wrapping each demo query — these appear alongside
        # the auto-instrumented GenAI spans in App Insights.
        with tracer.start_as_current_span(
            f"contoso-demo-query-{i}",
            attributes={
                "contoso.query.index": i,
                "contoso.query.text": query,
                "contoso.agent.name": AGENT_NAME,
            },
        ) as span:
            print(f"[{i}/{len(DEMO_QUERIES)}] Sending: {query}")
            start = time.time()

            try:
                response = run_agent_query(project, AGENT_NAME, query, AGENT_VERSION)
                elapsed = time.time() - start

                span.set_attribute("contoso.response.length", len(response))
                span.set_attribute("contoso.response.latency_ms", int(elapsed * 1000))

                # Print first 200 chars of response
                preview = response[:200].replace("\n", " ")
                print(f"  [OK] Response ({elapsed:.1f}s): {preview}...")
                print(f"  Trace ID: {span.get_span_context().trace_id:032x}")

            except Exception as e:
                span.set_attribute("contoso.error", str(e))
                span.record_exception(e)
                print(f"  [ERR] Error: {e}")

            print()

    print("-" * 60)
    print("Traces sent to Application Insights.")
    print("Wait 2-5 minutes, then check:")
    print("  Foundry portal > Operate > Assets > Agents > [your agent] > Traces")
    print("-" * 60)


if __name__ == "__main__":
    main()
