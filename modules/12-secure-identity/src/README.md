# Module 12 — Source

## IdentityInspector (.NET console + tests)

Offline JWT inspector that classifies a caller as **delegated user** (OBO),
**user**, or **service** (agent identity / managed identity) from its claims.
Reinforces the Module 12 point: *who is calling* is a claims decision.

```powershell
# Build + test
dotnet test IdentityInspector.Tests/IdentityInspector.Tests.csproj

# Run against a token (no signature validation — inspection only)
cd IdentityInspector
dotnet run -- <paste-a-jwt>
```

> The inspector never validates signatures. Use it to *understand* tokens, not to
> authenticate them.

## Live OBO / delegated demo

The attended (on-behalf-of) demo reuses the already-built Module 3 app:
[../../03-agentic-rag/src/track2-identity-rls](../../03-agentic-rag/src/track2-identity-rls).
Sign in as the NSW vs VIC estimator to see per-user row trimming with no filter
logic in the app.
