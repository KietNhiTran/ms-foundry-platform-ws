"""
CIMIC AI Gateway — Rate Limit Demo

Sends rapid-fire requests to a model deployment through the AI Gateway
to demonstrate rate limiting. The script shows:
  - Successful requests within the limit
  - HTTP 429 responses when the limit is exceeded
  - Raw HTTP status codes for each request

The script supports two modes:
  - APIM Gateway (via APIM_GATEWAY_URL + APIM_SUBSCRIPTION_KEY) — requests
    flow through APIM where policies are enforced
  - Direct (via PROJECT_ENDPOINT) — requests bypass APIM

Prerequisites:
  - AI Gateway enabled on the Foundry resource (Operate > Admin > AI Gateway)
  - Rate limit policy configured in APIM API-level policy:
      <rate-limit-by-key calls="5" renewal-period="60"
          counter-key="@(context.Subscription.Id)" />

Usage:
    1. Copy .env.example → .env and fill in values
    2. pip install -r requirements.txt
    3. python test_rate_limit.py

See Module 08 for full context.
"""

import json
import os
import time

import requests as http_requests
from dotenv import load_dotenv

load_dotenv()

from azure.identity import DefaultAzureCredential

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
PROJECT_ENDPOINT = os.environ["PROJECT_ENDPOINT"]
MODEL_DEPLOYMENT = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-5.4")
NUM_REQUESTS = int(os.environ.get("NUM_REQUESTS", "15"))
PROMPT = "Summarise CIMIC Group's key operating divisions in one paragraph."

# APIM Gateway — set to route through APIM for policy enforcement
APIM_GATEWAY_URL = os.environ.get("APIM_GATEWAY_URL", "")
APIM_SUBSCRIPTION_KEY = os.environ.get("APIM_SUBSCRIPTION_KEY", "")
API_VERSION = os.environ.get("API_VERSION", "2024-12-01-preview")

# ---------------------------------------------------------------------------
# Build request URL and headers
# ---------------------------------------------------------------------------
credential = DefaultAzureCredential()
token = credential.get_token("https://cognitiveservices.azure.com/.default").token

if APIM_GATEWAY_URL:
    url = (
        f"{APIM_GATEWAY_URL}/openai/deployments/{MODEL_DEPLOYMENT}"
        f"/chat/completions?api-version={API_VERSION}"
    )
    headers = {
        "Authorization": f"Bearer {token}",
        "api-key": APIM_SUBSCRIPTION_KEY,
        "Content-Type": "application/json",
    }
    routing_mode = f"APIM Gateway ({APIM_GATEWAY_URL})"
else:
    # Direct — extract base from PROJECT_ENDPOINT
    base = PROJECT_ENDPOINT.split("/api/projects/")[0]
    url = (
        f"{base}/openai/deployments/{MODEL_DEPLOYMENT}"
        f"/chat/completions?api-version={API_VERSION}"
    )
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
    }
    routing_mode = f"Direct ({base})"

# ---------------------------------------------------------------------------
# Rapid-fire requests to demonstrate rate limiting
# ---------------------------------------------------------------------------
def main():
    print(f"{'='*60}")
    print(f"  AI Gateway — Rate Limit Demo (Module 08)")
    print(f"{'='*60}")
    print(f"  Model: {MODEL_DEPLOYMENT}")
    print(f"  Requests: {NUM_REQUESTS}")
    print(f"  Routing: {routing_mode}")
    print(f"  URL: {url}")
    print(f"  Prompt: {PROMPT[:60]}...")
    print()

    body = {
        "messages": [{"role": "user", "content": PROMPT}],
        "max_completion_tokens": 100,
    }

    success_count = 0
    rate_limited_count = 0
    total_tokens_used = 0

    for i in range(1, NUM_REQUESTS + 1):
        start = time.time()
        response = http_requests.post(url, headers=headers, json=body)
        elapsed = time.time() - start
        status = response.status_code

        if status == 200:
            data = response.json()
            tokens_used = data.get("usage", {}).get("total_tokens", 0)
            total_tokens_used += tokens_used
            content = data["choices"][0]["message"]["content"][:80]
            print(
                f"  [{i:2d}/{NUM_REQUESTS}] ✅ {status} "
                f"({elapsed:.1f}s, {tokens_used} tokens)"
            )
            success_count += 1
        elif status == 429:
            rate_limited_count += 1
            retry_after = response.headers.get("Retry-After", "?")
            print(
                f"  [{i:2d}/{NUM_REQUESTS}] ⛔ {status} Rate Limited "
                f"(retry after {retry_after}s)"
            )
        else:
            error_text = response.text[:200]
            print(f"  [{i:2d}/{NUM_REQUESTS}] ❌ {status}: {error_text}")

    # ── Summary ──────────────────────────────────────────────────────
    print(f"\n{'─'*60}")
    print(f"  Results:")
    print(f"    Successful:    {success_count}")
    print(f"    Rate-limited:  {rate_limited_count}")
    print(f"    Other errors:  {NUM_REQUESTS - success_count - rate_limited_count}")
    print(f"    Total tokens:  {total_tokens_used}")
    print(f"{'─'*60}")

    if rate_limited_count > 0:
        print(f"\n  ✓ Rate limiting is working! {rate_limited_count} requests were throttled.")
        print(f"  Enforced by: APIM rate-limit-by-key policy")
        print(f"    Azure portal > APIM > APIs > Policies")
    else:
        print(f"\n  ℹ  No rate limiting observed. Either:")
        print(f"    - The call limit is set higher than {NUM_REQUESTS} requests/min")
        if not APIM_GATEWAY_URL:
            print(f"    - Requests are going DIRECT (bypassing APIM)")
            print(f"      Set APIM_GATEWAY_URL in .env to route through the gateway")
        print(f"    Try reducing the call limit or increasing NUM_REQUESTS")


if __name__ == "__main__":
    main()
