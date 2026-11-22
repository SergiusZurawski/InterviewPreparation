# 07 · AI-Assisted Review Workflow (Claude Code / ChatGPT)

> TRG: *"The goal isn't to see how well you can use an AI tool, but how effectively you pair it with your own expertise."*
> They score: **how you give context**, **how you evaluate suggestions**, **ownership**.

## 1. Setup (do this the day before)

**Option A: Claude Code (recommended).** It can read the file, you can ask follow-ups about specific lines, and it can write a scratch fix or test.
- Create an empty folder, e.g. `C:\interview\trg-exercise\`. Open a terminal there and run `claude`. Save the markdown from the chat into it.
- Nothing else in that folder or parent `CLAUDE.md` files. Check that no global memory or CLAUDE.md content leaks employer info onto the screen.
- Close all work projects, Slack, Teams, browser tabs with employer content. **Nothing from your current employer on screen or in prompts.**
- Font size large enough for screen share; dark/light theme readable.
- Have a .NET SDK installed in case you want to compile a snippet (`dotnet new console`). Optional but a strong signal.

**Option B: ChatGPT in a browser** plus the markdown open in VS Code side by side. Fallback if Claude Code has problems.

Test screen sharing + rejoining the call beforehand (the guide warns about this).

## 2. Context prompt template (adapt live, don't paste blindly)

```
You are helping me review a pull request. I will make the final decisions.

Context:
- Stack: C# / .NET (ASP.NET Core Web API, EF Core, SQL Server). Assume production, multiple instances behind a load balancer, multi-tenant.
- PR intent: <one sentence from the PR description>
- The file is <exercise.md>, which contains the description and the diff.

Task:
Review the diff for: correctness bugs, edge cases, EF Core data-access problems (N+1, tracking, client-side evaluation, transactions, concurrency), async misuse, security (authorization/tenant isolation, injection, data exposure), error handling, performance, readability and missing tests.

Output format:
- A table: severity (blocker / should-fix / nit), file:line, issue, why it matters (concrete failure scenario), suggested fix.
- Separately list anything you're UNSURE about, and what information would confirm it.
- Don't rewrite the whole code. Don't invent requirements.
```

Why this works (say it out loud): stack plus deployment assumptions, intent, explicit categories, **evidence-based output with line numbers**, uncertainty flagged separately, scope limits.

## 3. Follow-up prompts that show judgment
- "For finding #3, show me exactly which line causes it and describe a concrete input that triggers it."
- "You said this is a race condition. The DbContext is scoped per request, so explain how two threads would share it, or withdraw the finding."
- "Which of these findings would you drop if you could only raise five? Rank them by production impact."
- "What did you NOT check? Which assumptions are you making about code not shown?"
- "Propose the minimal fix for #1 only. Keep the public API unchanged."
- "Write an xUnit test that fails on the current code and passes with the fix."
- "Is `<API>` real in EF Core 8, and what does it do exactly?" (then check yourself)

## 4. How AI goes wrong in reviews (and how you catch it)

| Failure mode | Example | Your check |
|---|---|---|
| **False positive** | Flags a "race condition" in request-scoped code | Reason about lifetimes/threads yourself |
| **Hallucinated / outdated API** | Suggests a method that doesn't exist, or EF Core 2 behaviour (silent client eval) | Check the EF version and docs; say "I'd verify this in the docs" |
| **Misses business logic** | Doesn't know that inclusive end dates double-count | You bring the domain reasoning |
| **Misses cross-file context** | Doesn't connect DI registration in Program.cs with the service's lifetime | Trace data flow yourself |
| **Fix introduces new bug** | Fixes N+1 by loading the whole table | Review the fix as a new diff |
| **Over-engineering** | Suggests repository + unit of work + MediatR for a 20-line change | YAGNI. Smallest change that's correct |
| **Severity inflation** | Everything is "critical" | You prioritise |
| **Generic advice** | "Consider adding logging" | Ask for concrete, line-specific findings |
| **Tests that pass trivially** | Asserts the mocked return value | Ask "would it fail if the code were broken?" |

Your real experience matches this: in the Claims revival **the AI got confused and you had to correct the knowledge base by manual verification** (see [08](08-behavioural-stories.md)).

## 5. Think-aloud phrases
- "Before I ask the AI, let me form my own view…"
- "I'm giving it the deployment context because lifetime and concurrency findings depend on it."
- "It's flagged eight things. Three I agree with, two are false positives, and here's why…"
- "I don't fully trust this claim. Let me check it against the code."
- "This is plausible, but I'd verify it with a test before raising it as a blocker."
- "AI missed this one: the date range end is inclusive, which double-counts events at midnight."
- "If I were approving this PR, my name is on it too, so here's what must change before I approve."

## 6. Anti-patterns to avoid on screen
- Pasting the whole diff with "find bugs" and reading the answer aloud.
- Long silences while the AI generates. Keep narrating your own analysis in the meantime.
- Accepting a large AI-rewritten file. Prefer small, explainable changes.
- Arguing with the AI for a long time. Decide, explain, move on.
- Forgetting tests in the final verdict.

## 7. Your AI workflow story, short version (for "how do you use AI day to day?")
> "I use AI as a fast junior pair with broad knowledge but no accountability. I give it context and constraints, ask for evidence, and verify claims by running the code, reading docs, or writing a test. I use it most for orienting myself in unfamiliar or legacy code, generating test scaffolding, and second-pass reviews. I'm careful with security-sensitive code and anything where it can't see the whole system. And I never paste employer-confidential code into tools that aren't approved."
