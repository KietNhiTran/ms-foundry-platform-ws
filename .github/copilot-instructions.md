# Workshop Content Development Guidelines

This file provides instructions for GitHub Copilot when assisting with workshop content creation.

---

## Workshop Content Principles

### 1. Audience-First Design

- **Identify the audience** before writing: developers, architects, security teams, executives
- **Match depth to audience**: executives need outcomes, developers need code samples
- **Use role-appropriate language**: avoid jargon for non-technical audiences

### 2. Modular Structure

- Design workshops as **independent modules** that can be mixed and matched
- Each module should be **self-contained** with clear prerequisites
- Provide **multiple track configurations** (half-day, full-day, role-specific)
- Include estimated **duration** for each module

### 3. Fictional Scenarios

- Use **fictional company names** (Contoso, Fabrikam, Northwind) for reusable content
- Create **relatable use cases** that translate across industries
- Avoid customer-specific details unless explicitly requested

### 4. Code Quality & Testing

- **All demo code must be runnable** — Copilot must test and verify code before committing
- Include **automated tests** in GitHub Actions workflow to prevent regression
- Each module's `src/` code should include:
  - Unit tests or smoke tests
  - CI workflow that runs on PR and push
  - Clear error messages for common failures

### 5. Infrastructure as Code (Per Module)

- Each module requiring Azure resources must include **independent infra code**
- Store infra in `modules/XX-name/infra/` folder
- Support **isolated deployment** — module infra should be deployable without other modules
- Provide:
  - `main.bicep` for resource provisioning
  - `deploy.ps1` for one-click deployment
  - `teardown.ps1` to clean up resources after demos

### 6. Source Code Language

- Demo source code should be **either .NET or Python** (author's choice per module)
- Authors can adjust the language when creating workshop content based on audience
- Ensure consistency within a single module — don't mix languages in one module
- Include language-appropriate dependency files:
  - Python: `requirements.txt`
  - .NET: `*.csproj`

---

## Content Formatting Standards

### Module Structure

Each module README must include: **Title + Duration**, **Objective** (one sentence), **Topics** (bullet list), **Demo** description, **Reference** link to Microsoft Learn.

### Pre-Demo Checklists

For presenter-led demos, include a setup checklist table with columns: #, Task, How, Verify.

### Time Estimates

Concept overview: 10-15 min | Portal walkthrough: 10-15 min | Code demo: 15-20 min | Hands-on exercise: 20-30 min | Q&A buffer: 5-10 min per module

---

## Documentation References

- Use **Microsoft Learn** as the authoritative source: `[Display Text](https://learn.microsoft.com/path/to/doc)`
- Verify links are current before publishing
- Prefer **concept pages** over API reference for workshops
- Use tables for reference link collections

### Documentation Verification (Required)

When creating or updating module content, **always verify against the latest Microsoft Learn documentation** before writing:

1. **Before writing any Foundry content**, fetch the relevant Microsoft Learn page to confirm current terminology, API surface, and feature availability.
2. **Use the fetch MCP tool** (in cloud) or **Microsoft Learn MCP tools** (in VS Code) to search and retrieve docs:
   - Search: query `learn.microsoft.com` for the topic
   - Fetch: retrieve the full page for detailed verification
3. **Key pages to check** before each module:
   - Foundry overview: `https://learn.microsoft.com/azure/foundry/what-is-foundry`
   - Agent service: `https://learn.microsoft.com/azure/foundry/agents/concepts/workflow`
   - SDK reference: `https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview`
   - Migration guide (terminology): `https://learn.microsoft.com/azure/foundry/how-to/navigate-from-classic`
4. **If a doc page returns 404 or has changed**, update the terminology table and key links in this file.
5. **Never assume** — Foundry is evolving rapidly. Features marked "Preview" may have gone GA or been renamed since this file was last updated.

---

## Workshop Delivery Formats

- **Led Demo** (recommended): Presenter demonstrates, audience observes. Mark as "Led Demo" in agenda.
- **Hands-On Lab**: Attendees follow along. Requires prerequisites, troubleshooting, and checkpoint steps.
- **Hybrid**: Presenter demos + simplified attendee exercises. Best for small mixed-skill groups.

---

## Content Quality Checklist

Before finalizing workshop content:

- [ ] **Audience identified** — Who is this for?
- [ ] **Objectives clear** — What will they learn?
- [ ] **Prerequisites listed** — What do they need beforehand?
- [ ] **Modules are independent** — Can they be reordered?
- [ ] **Durations realistic** — Include buffer time
- [ ] **Links verified** — All documentation links work
- [ ] **Fictional company used** — No customer-specific details (unless requested)
- [ ] **Diagrams included** — Visual aids for architecture concepts
- [ ] **Code samples tested** — All demos verified working
- [ ] **Glossary provided** — Define domain-specific terms

---

## Diagram Guidelines

- Use **Mermaid** for inline diagrams (flows, decisions, sequences)
- Use **images** for complex architecture, screenshots, and portal UI walkthroughs
- Conventions: top-to-bottom for data flow, left-to-right for process flow, subgraphs for groupings

---

## Microsoft Foundry-Specific Guidelines

When creating Foundry workshop content:

### Terminology (Current)

Based on [Microsoft Foundry migration guide](https://learn.microsoft.com/azure/foundry/how-to/navigate-from-classic) (updated May 2026):

| Current Term | Previous Term(s) |
|--------------|------------------|
| Microsoft Foundry | Azure AI Studio, Azure AI Foundry |
| Foundry resource | Hub + Azure OpenAI + Azure AI Services |
| Foundry Tools | Azure AI Services, Azure Cognitive Services |
| Foundry Direct Models | Model-as-a-Service (MaaS) |
| Project | Workspace (in classic) |
| Responses API | Assistants API (sunset: Aug 26, 2026) |
| Agent Versions | Assistants, Agents (classic) |
| Conversations | Threads |
| Items | Messages |
| Responses | Runs |
| Foundry User | Azure AI User, Cognitive Services OpenAI User |
| Foundry Owner | Azure AI Owner |
| Foundry Account Owner | Azure AI Account Owner |
| Foundry Project Manager | Azure AI Project Manager |
| `azure-ai-projects` 2.x | `azure-ai-projects` 1.x, `azure-ai-generative` |
| `OpenAI()` client | `AzureOpenAI()` client (`azure-ai-inference` retiring May 30, 2026) |

> **Note:** The Foundry RBAC role rename is rolling out. You may still see "Azure AI User" etc. in some Azure portal surfaces. The role IDs and permissions are unchanged.

### Key Documentation Links

| Topic | URL |
|-------|-----|
| What is Foundry? | https://learn.microsoft.com/azure/ai-studio/what-is-ai-studio |
| Migrate from Classic | https://learn.microsoft.com/azure/foundry/how-to/navigate-from-classic |
| Agent Service | https://learn.microsoft.com/azure/foundry/agents/concepts/workflow |
| Tool Catalog | https://learn.microsoft.com/azure/foundry/agents/concepts/tool-catalog |
| SDK Overview | https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview |
| Content Safety | https://learn.microsoft.com/azure/ai-services/content-safety/overview |
| Foundry IQ | https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq |
| Control Plane | https://learn.microsoft.com/azure/foundry/control-plane/overview |

---

## File Organization

```text
workshop-repo/
│
├── .github/
│   └── copilot-instructions.md      # This file (Copilot guidelines)
│
├── workshop-[topic].md              # Main workshop agenda
│
├── modules/                         # Individual module content
│   ├── 01-foundry-setup/
│   │   ├── README.md                # Module content and instructions
│   │   ├── src/                     # Demo source code for this module
│   │   │   ├── app.py
│   │   │   ├── requirements.txt
│   │   │   └── test_app.py          # Tests for this module
│   │   ├── data/                    # Sample data for this module
│   │   │   └── sample-config.json
│   │   ├── docs/                    # Optional: module-specific deep-dive docs
│   │   │   └── topic-explainer.md
│   │   └── infra/                   # Infrastructure code (independent)
│   │       ├── main.bicep           # Bicep template for resources
│   │       ├── deploy.ps1           # One-click deploy script
│   │       └── teardown.ps1         # Cleanup script
│   │
│   └── ...                          # All modules follow the same pattern
│
└── shared/                          # Shared resources across all modules
    ├── src/
    │   └── common.py
    ├── data/
    │   └── eval-dataset.jsonl
    ├── docs/                        # Cross-cutting reference documents
    │   ├── networking.md
    │   ├── security.md
    │   └── disaster-recovery.md
    └── sample-policy/               # Organization-wide sample policies
        └── governance-framework.md
```

### Reference Folder (`ref/`)

> **⚠️ The `ref/` folder if it exists, it contains content and source code from a previous workshop iteration. It is must NOT be used directly.**

Rules for handling `ref/` content:

1. **Never copy content verbatim** from `ref/` into new modules or `shared/docs/`.
2. **Treat as reference only** — review if it is relevant content. if a topic in `ref/` is relevant to a new module, use it as a starting point but:
   - Verify all information against the **latest Microsoft Learn documentation**
   - update source code accordingly to choosen language of the workshop
   - Update terminology to match the current terminology table in this file
   - Rewrite to fit the target module's structure
3. **Place reviewed content** in the appropriate location:
   - Module-specific deep-dives → `modules/XX-name/docs/`
   - Cross-cutting enterprise topics → `shared/docs/`
   - source code in module src folder.
4. **Do not reference `ref/` files** in any workshop agenda, module README, or presenter guide.
5. **Assume outdated** — the `ref/` content uses older Foundry terminology (e.g., "Azure AI User" instead of "Foundry User", "Foundry Account" instead of "Foundry resource") and may describe deprecated APIs or features.

---

## Common Patterns

- **Introducing a concept**: What → Why → How → Demo → Reference link
- **Comparing options**: Use feature comparison tables with ✅/❌
- **Decision trees**: Use Mermaid flowcharts

---

## Tone and Style

Concise, practical, confident, inclusive, action-oriented. Use verbs: "Create", "Configure", "Verify".

---

## Git Conventions

- Branches: `feature/module-XX-desc`, `fix/issue-desc`, `customer/company-name`
- Commits: conventional commits (`feat:`, `fix:`, `docs:`), reference module number
- Always include `.env.example` with placeholder values in demo code

---

## Versioning

- Include **version number** and **last updated date** at top of workshop files
- Note **platform version** (e.g., "Foundry V2") when relevant
- Flag content that may change: "⚠️ Preview feature — may change"
