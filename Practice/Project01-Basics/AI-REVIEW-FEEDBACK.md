> Copied from `D:\WorkSpace\interviewPrep\AI-Review-01\PROMPT-FEEDBACK.md`. That folder (outside this repo) also has the session transcript and the AI-generated `ai_help/` files referenced below.

# Prompt Feedback: AI Review 01 (replay of PR #212)

Source: the session transcript (5 prompts, about 16:44 to 17:09, roughly 25 minutes) and the files in `ai_help/`.

## TL;DR

**The AI found all 15 planted issues and several valid extras. The review it produced would pass. Your part in it would not, yet.**

You set up a sensible **process** (understand the app, then the PR, then a plan, then the review), and that's a good instinct. After the first prompt, though, you acted as a **dispatcher**: "yes create", "ok, proceed". You never **checked, challenged, ran or rewrote** anything the AI said. The interview scores exactly those things. With AI available to everyone, the question is not "can the AI find bugs?" but "**can this person tell when the AI is right, wrong or overdoing it, and own the final call?**"

## Scorecard (interview criteria)

| Criterion | Score | Evidence |
|---|---|---|
| **Structure / process** | 🟢 Good | Staged approach in prompt 1; plan before review. |
| **Briefing the AI** (context, constraints, format) | 🟡 Partial | You pointed it at PR.md, which was good. You gave no severity scale, no size limit, no time budget and no "don't edit code". So the AI invented a 5-level scale plus P0 to P3, and wrote 1,151 lines. |
| **Verifying AI claims** | 🔴 Missing | The AI said twice that it *"didn't build or run the project"*. You didn't ask it to. Zero claims were tested. |
| **Challenging / pushing back** | 🔴 Missing | No "are you sure?", no questioning of severities, no "what did you miss?". |
| **Prioritisation / signal vs noise** | 🔴 Missing | You accepted 30 findings, 13 of them "must fix before merge". There's no top-5. |
| **Owning the verdict** | 🔴 Missing | The verdict, summary and PR comments are all AI-written. Nothing is in your own words. |
| **Time management** | 🟡 | See the timeline. Your reading time went the wrong way round. |
| **Staying on task** | 🟡 | Prompt 5 (the generic top-20 watchlist) is good for study but drifts from the task in an interview. |

## Timeline: where your attention went

| Step | AI time | Your time with the output before the next prompt |
|---|---|---|
| 01 app overview (139 lines) | 46s | **~6 min** |
| 02 PR understanding (162 lines) | 57s | **~5 min** |
| 03 review plan (178 lines) | 47s | **~4 min** |
| 04 **findings (478 lines, 35 KB)** | 2m48s | **~1 min** ← the most important output got the least reading |
| 05 top-20 watchlist | 1m | (end) |

You spent about 15 minutes on context and about 1 minute on the findings. In the interview, flip it: keep the context short (5 min) and spend most of your time **inside the findings**, verifying and ranking them.

## Prompt by prompt

### Prompt 1: the kickoff
> *"read PR.md and read PR, then code. We would like to do a review, however first we want to understand what the project is doing, then … plan … then do review and highlight issues with priority severity options to fix. So first create file … Create folder ai_help exclude from git …"*

**Good**
- A staged process, and you explicitly said "first understand, then plan, then review". Interviewers like seeing a method.
- A knowledge-base folder kept out of git is a good habit on a **large** codebase.

**Improve**
- **No role or stakes.** Say who the reviewer is and what matters ("senior .NET reviewer, multi-tenant financial SaaS, security and correctness first").
- **No severity scale.** Your team uses **Blocker / Should fix / Nit / Question**. Because you didn't say so, the AI made up Blocker/Critical/Major/Minor/Nit plus P0 to P3, and now you have to map it back.
- **No size limits.** "Create a file describing the application" turned into 139 lines. For 8 files, one paragraph is enough. Ask for "max 15 lines".
- **No "read-only" rule.** Say "don't modify the code under review." It didn't edit anything this time, but it's a good guard.
- **Too many instructions in one breath, with typos.** That's fine for the AI, but in the interview the interviewer reads your prompts. Clear, structured prompts are part of how they judge you.

### Prompt 2: "Check what PR changes and create a knowledge base for PR"
- Fine, but here the AI **already did the review** (02 lists essentially every bug). This was the moment to adapt: *"You've already listed issues. Skip the plan file and go straight to verifying and ranking these."* The plan step after this was ceremony.

### Prompt 3: "yes create Next is 03-review-plan.md"
### Prompt 4: "ok, proceed with 04-review-findings.md"
- These are **rubber stamps**. The AI is driving and you are approving. Each "proceed" was a chance to add judgement, for example:
  - "Use our 4 labels only. Max 10 findings in the main list; everything else goes into a 'nits / follow-ups' appendix."
  - "For each finding, give your confidence (high/med/low) and how we could prove it."
  - "Build the project and tell me if it compiles."

### Prompt 5: "add file of main issues… top 20 so we watch for them"
- It's a useful study artefact, so keep it for your prep. In the interview, though, it does nothing for **this** PR and costs time.
- It also contains a claim to challenge: *"PR #212 hits all 20 issue types"*. Then it admits item 20 isn't fully present, and item 9 only applies through `TrustServerCertificate`. A "100% hit rate" is an **AI over-claiming smell**. Say so out loud: "that sounds too neat, let's check."

## What you should have challenged (concrete examples from 04)

These are the moments where an interviewer would want to see you step in:

1. **Severity inflation.** 6 Blockers + 7 Criticals = 13 "must fix before merge". Is **PII in logs** (B6) really the same level as **SQL injection** (B2)? Is **local time instead of UTC** (C2) "Critical"? Push back: *"Rank them. If the author could fix only 5 things today, which 5?"* My answer would be: secret, tenant isolation (with auth), SQL injection, overposting, captive DbContext.
2. **Duplicates.** n3 (`!` operator) and C5 (500 instead of 404) are the same bug. n1 (comments contradict code) overlaps C4 (sort order). Merge them; the author shouldn't get the same problem twice.
3. **"The app won't start in Development"** (C1). That's a **testable** claim. `dotnet build` and `dotnet run` take about 30 seconds, and it becomes a fact instead of a "should". It's the single best verification move available in this PR.
4. **Integer division / inclusive end date.** Ask the AI for a 10-line xUnit test or a `dotnet script` snippet that proves `30/100*100 == 0` and that an event at `2026-11-30T15:00` is dropped. Seeing it fail is the strongest kind of review comment.
5. **"Not built or run".** When the AI tells you its own limitation, it's handing you the next prompt. Take it.
6. **30 findings for the author.** A real author receiving 30 comments plus a 478-line document will push back. Ask for **a short PR comment set** (top issues plus one grouped "also consider" comment).

## The final output you're missing

In the interview, finish with **your own** 30-second summary, spoken or written by you, not pasted:

> "Request changes. Five blockers: the prod password is committed (rotate it now), there's no `[Authorize]` and the tenant comes from the query string or isn't filtered (cross-tenant data leak), SQL injection in search, overposting lets clients set `IsBillable`/`TenantId`, and a singleton captures the DbContext. Then correctness against the ACs: UTC, inclusive end date, sort order, 404, integer-division percentages. I verified the DI failure and the percentage bug by running them. The rest are should-fix items, plus tests and a migration."

Then ask the AI to **critique your summary**, not to write it.

## Reusable prompt kit for the interview

Adapt these. The quality comes from the constraints, not from the wording.

**1. Brief (once, at the start)**
```
You're helping me review a PR as a senior .NET reviewer. Context: <one line about the domain>.
Inputs: PR.md (ticket, acceptance criteria, conventions) and the diff/code in <path>.
Rules: don't modify the code under review. Severity labels: Blocker / Should fix / Nit / Question.
Every finding needs file:line, problem, impact (a concrete failure example), fix, and your confidence (high/med/low).
Keep outputs short; I'll ask for detail when I need it.
First: in max 10 lines, tell me what the app does and what this PR changes. No findings yet.
```

**2. Checklist from the ticket (before reading findings)**
```
Turn each acceptance criterion and convention in PR.md into a checklist item.
For each one: met / not met / unclear, with file:line. Table only.
```

**3. Focused review passes (one area at a time, so you can keep up)**
```
Pass 1, security and tenant isolation only. For each endpoint: where does tenantId come from, and is it in the query?
Max 8 findings, ranked.
```

**4. Challenge (always do at least one)**
```
Which of your findings are you least confident about, and why? Which could be false positives?
What would you check to confirm each one?
```
```
Do a second pass for things you may have missed: arithmetic, date boundaries, null paths, status codes, async.
Only report NEW findings.
```

**5. Verify (turn claims into evidence)**
```
Build the project. Then write the smallest xUnit test (or script) that proves finding <X>. Run it and show me the output.
```

**6. Prioritise and finish**
```
If the author can fix only 5 things before merge, which 5 and why? Merge duplicates.
Then draft max 6 PR comments in a polite reviewer tone. I'll edit them.
```
```
Here's my verdict and summary: <yours>. Critique it. What's wrong, missing or over-stated?
```

## Habits to carry into Projects 05–06 (the AI mocks)

1. **Brief once, with constraints**: role, inputs, severity labels, output format, length, read-only.
2. **Never say "proceed" without adding judgement.** Every follow-up prompt should narrow, challenge, verify or rank.
3. **Turn at least 2 claims into evidence** (build, run, a tiny test). Say out loud: *"Let me verify this rather than trust it."*
4. **When the AI states a limitation ("not run", "assuming"), turn it into your next prompt.**
5. **Spend your reading time on the findings**, not on the context files.
6. **Cut the noise:** top 5 plus a grouped appendix, merge duplicates, push back on inflated severity.
7. **Finish in your own words**, then let the AI critique you, not the other way round.
