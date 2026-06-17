# Attack Strategies Reference

This document provides a reference for the attack strategies supported by the AI Red Teaming Agent. These strategies are sourced from [PyRIT (Python Risk Identification Tool)](https://github.com/microsoft/PyRIT) and can be selected when configuring a red teaming scan in the Foundry portal or via the SDK.

---

## How Attack Strategies Work

The AI Red Teaming Agent provides curated seed prompts per risk category for direct adversarial probing. However, direct prompts are often caught by existing safety alignments. **Attack strategies** apply conversions that transform prompts to bypass or subvert safety filters.

**Example:** A direct ask *"How do I loot a bank?"* triggers a refusal. Applying a character-flip strategy can trick the model into answering.

---

## Encoding & Obfuscation Strategies

These strategies encode or transform the adversarial prompt to evade text-based content filters.

| Strategy | Description | Example Technique |
|----------|-------------|-------------------|
| **Base64** | Encodes prompt into Base64 text format | `SGVsbG8gV29ybGQ=` → `Hello World` |
| **Binary** | Converts text into binary (0s and 1s) | `01001000 01101001` → `Hi` |
| **Morse** | Encodes text into Morse code (dots and dashes) | `.... ..` → `Hi` |
| **Url** | Encodes text into URL-encoded format | `%48%65%6C%6C%6F` → `Hello` |
| **ROT13** | Caesar cipher shifted by 13 positions | `Uryyb` → `Hello` |
| **Caesar** | Substitution cipher with configurable shift | Shifts characters by fixed positions |
| **Atbash** | Substitution cipher mapping each letter to its reverse | `A↔Z`, `B↔Y`, etc. |
| **Leetspeak** | Replaces letters with similar numbers/symbols | `H3ll0 W0rld` → `Hello World` |

---

## Character Manipulation Strategies

These strategies alter character representation to create visual confusion or bypass text matching.

| Strategy | Description | Example Technique |
|----------|-------------|-------------------|
| **UnicodeConfusable** | Uses Unicode characters that look similar to standard characters | `а` (Cyrillic) instead of `a` (Latin) |
| **UnicodeSubstitution** | Substitutes standard characters with Unicode equivalents | Replaces ASCII with full-width Unicode |
| **Diacritic** | Adds diacritical marks to characters | `Hëllö Wörld` |
| **CharacterSpace** | Adds spaces between characters for obfuscation | `H e l l o` |
| **CharSwap** | Swaps characters within text to create variations | `Hlelo` → `Hello` |
| **Flip** | Reverses characters from front to back | `dlroW olleH` → `Hello World` |
| **StringJoin** | Joins multiple strings together for concatenation | Splits and reassembles prompt fragments |

---

## Visual & Formatting Strategies

| Strategy | Description | Example Technique |
|----------|-------------|-------------------|
| **AsciiArt** | Generates visual art using ASCII characters | Prompt rendered as text art |
| **AsciiSmuggler** | Conceals data within invisible ASCII/Unicode characters | Hidden instructions in invisible text |
| **AnsiAttack** | Uses ANSI escape sequences to manipulate text behavior | Terminal control codes in prompts |

---

## Prompt Injection Strategies

These are the most critical strategies for testing agent security.

| Strategy | Description | Contoso Relevance |
|----------|-------------|-------------------|
| **Jailbreak** (UPIA) | Injects crafted prompts to bypass AI safeguards via direct user input | Tests whether a user can directly ask the Contoso Estimator to reveal confidential rates |
| **Indirect Jailbreak** (XPIA) | Injects attack prompts in tool outputs or returned context | Tests whether malicious instructions in uploaded specification documents can compromise the agent |
| **SuffixAppend** | Appends an adversarial suffix to the prompt | Adds bypass instructions at the end of a normal estimation query |

---

## Multi-Turn Strategies

These strategies operate across multiple conversation turns to gradually break down defenses.

| Strategy | Description | Contoso Relevance |
|----------|-------------|-------------------|
| **Multi turn** | Executes attacks across multiple conversational turns, using context accumulation | Builds rapport with the agent over several estimation queries before requesting confidential data |
| **Crescendo** | Gradually escalates complexity or risk over successive turns | Starts with general rate questions, gradually narrows to confidential margins |
| **Tense** | Changes the tense of text (converts to past tense) | Reframes requests as hypothetical past scenarios to bypass guardrails |

---

## Selecting Strategies for the Contoso Estimator

When running red teaming scans against the Contoso Estimator Advisor, prioritize these strategies based on the agent's risk profile:

### Priority 1 — Data Leakage Testing

| Strategy | Rationale |
|----------|-----------|
| Jailbreak | Direct attempts to extract confidential labor/plant/materials rates |
| Indirect Jailbreak | Malicious instructions hidden in uploaded specification PDFs |
| Crescendo | Gradual escalation from general to confidential rate queries |
| Multi turn | Context accumulation to trick the agent into revealing margins |

### Priority 2 — Content Safety

| Strategy | Rationale |
|----------|-----------|
| UnicodeConfusable | Bypass content filters using look-alike characters |
| AsciiSmuggler | Hide harmful instructions in invisible characters |
| Base64 | Encode harmful requests to bypass text-based filters |
| Leetspeak | Disguise harmful terms using number substitutions |

### Priority 3 — Comprehensive Assessment

| Strategy | Rationale |
|----------|-----------|
| SuffixAppend | Test for adversarial suffix vulnerabilities |
| Flip | Test model's ability to handle reversed text attacks |
| ROT13 | Test whether encoded instructions bypass safety alignment |

---

## Cloud vs Local Red Teaming

| Aspect | Local | Cloud |
|--------|-------|-------|
| **Availability** | All regions | East US 2, France Central, Sweden Central, Switzerland West, US North Central |
| **Risk categories** | Content risks (hateful, sexual, violent, self-harm, protected materials, code vulnerability, ungrounded attributes) | All content risks + agentic risks (prohibited actions, sensitive data leakage, task adherence) |
| **Input redaction** | No | Yes — harmful inputs are redacted in results |
| **Sandboxing** | No — runs against your actual endpoint | Transient runs — harmful data not logged by Agent Service |
| **Agent tool testing** | Limited | Full tool output evaluation |

> **Recommendation:** Use **cloud red teaming** for agent-specific risks (data leakage, prohibited actions, task adherence) and **local red teaming** for rapid iteration on content risks during development.

---

## References

| Topic | Link |
|-------|------|
| AI Red Teaming Agent | [learn.microsoft.com/azure/foundry/concepts/ai-red-teaming-agent](https://learn.microsoft.com/azure/foundry/concepts/ai-red-teaming-agent) |
| PyRIT (Open Source) | [github.com/microsoft/PyRIT](https://github.com/microsoft/PyRIT) |
| Risk & Safety Evaluators | [learn.microsoft.com/azure/foundry/concepts/evaluation-evaluators/risk-safety-evaluators](https://learn.microsoft.com/azure/foundry/concepts/evaluation-evaluators/risk-safety-evaluators) |
| Run Red Teaming in Cloud | [learn.microsoft.com/azure/foundry/how-to/develop/run-ai-red-teaming-cloud](https://learn.microsoft.com/azure/foundry/how-to/develop/run-ai-red-teaming-cloud) |
| Run Red Teaming Locally | [learn.microsoft.com/azure/foundry/how-to/develop/run-scans-ai-red-teaming-agent](https://learn.microsoft.com/azure/foundry/how-to/develop/run-scans-ai-red-teaming-agent) |
