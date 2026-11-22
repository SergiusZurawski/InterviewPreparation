# Knowledge Base — TRG Screen Interview (Activity Monitor, Senior Full Stack C#/.NET)

**Interview:** Thursday 26 November 2026 · 90 min
**Format:** intros → behavioural → **AI-assisted review of a C# PR (ASP.NET Core + EF Core)** → technical questions

## Files

| # | File | Use it for | Priority |
|---|---|---|---|
| 01 | [Code review playbook](01-code-review-playbook.md) | The process + checklist you run during the exercise | 🔴 Core |
| 02 | [EF Core for Dapper developers](02-ef-core-for-dapper-devs.md) | Your main technical gap | 🔴 Core |
| 03 | [ASP.NET Core review pitfalls](03-aspnet-core-pitfalls.md) | DI, async, HttpClient, validation, errors, background work | 🔴 Core |
| 04 | [C#/.NET fundamentals](04-csharp-dotnet-fundamentals.md) | Technical questions section | 🟠 High |
| 05 | [Testing & debugging](05-testing-and-debugging.md) | Test-gap spotting, xUnit vs NUnit, integration tests | 🟠 High |
| 06 | [Security checklist](06-security-checklist.md) | Security findings in the diff | 🟠 High |
| 07 | [AI-assisted review workflow](07-ai-assisted-review.md) | How to drive Claude Code / ChatGPT on screen | 🔴 Core |
| 08 | [Behavioural stories (STAR)](08-behavioural-stories.md) | The behavioural conversation | 🔴 Core |
| 09 | [Technical question bank](09-technical-questions.md) | Quick-fire Q&A revision | 🟠 High |

Source material: [`../Docs/`](../Docs/) — TRG guide, CV, full story bank.

## The one-sentence strategy

> Review like a senior engineer who **owns** the result: form your own hypotheses first, give the AI precise context, **verify every AI claim**, prioritise by impact, and narrate your reasoning the whole time.

## Context about the company (to be checked)

TRG Screen sells software that helps financial institutions manage their **spend on market data and research subscriptions, and their usage of those subscriptions**. "Activity Monitor" is most likely the product that tracks **how users actually use** those services. This is my inference and is **not confirmed**, so check it on their website and LinkedIn before the interview.

If the inference is right, expect code dealing with:
- **lots of events or log records** that the system takes in and stores
- **totals and summaries over time ranges**
- **paging and filtering** through results
- **jobs that run in the background**
- **separating each customer's data** (multi-tenancy)

This kind of code is where performance and data-correctness bugs tend to be.
