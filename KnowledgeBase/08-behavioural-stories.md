# 08 · Behavioural Stories (STAR)

> Format: **S**ituation → **T**ask → **A**ction (use "I", not only "we") → **R**esult (numbers if possible) → **L**earning.
> Target length: 2–3 minutes each. Full story bank: [`../Docs/QuestionsAnswers.md`](../Docs/QuestionsAnswers.md).
> 📝 = details you still need to fill in.

## TRG's explicit prompts → which story

| TRG prompt | Primary story | Backup |
|---|---|---|
| Something you **owned end to end** | PIAD | Claims revival |
| A time **requirements were unclear** | Quadriss | Claims revival (wrong expectations) |
| A time **your team changed how it worked** | Robot Framework → C# test framework | Code-review ownership change |
| Where **AI helped** | Claims revival | 📝 second example |
| Where **AI got it wrong / how you caught it** | Claims revival (AI confused, manual verification) | 📝 concrete small example |

---

## ⭐ Story A: Reviving the Claims project with AI (AI helped + AI was wrong + ownership)

**Situation**
- A Claims project had been **paused for ~4 years**.
- It had only ever been deployed to UAT on a shared environment. It never reached production.
- Product management and the engineering manager **expected it to be basically working** and ready to pick up again.

**Task**
- Get it running again.
- Establish the **real** state of the project.
- Give stakeholders a realistic plan and estimate.

**Action**
1. **Used AI to orient myself in an unfamiliar legacy codebase.**
   - I built a knowledge base iteratively with AI, one investigation stage at a time.
   - Each stage was a focused prompt: dependencies, startup configuration, flows, integrations.
2. **Got it running locally.**
   - AI helped me identify missing dependencies and configuration.
   - I then **validated its findings by sending real requests to the locally running server** rather than trusting the analysis.
3. **Found the gap between expectation and reality:**
   - Internal dependencies such as the **batch processor** had changed in the meantime.
   - Several **libraries were outdated or incompatible**.
   - **The functional tests were giving false positives.** They reported success even though the flow contained errors, so the "green" status was misleading.
4. **Collected evidence before going to stakeholders:**
   - dependencies
   - reproductions
   - facts
5. **Went to the Product Owner with that evidence** and proposed focusing on the **three core flows: Submit claim, Cancel claim, Adjust/Repair**.
   - They agreed.
   - They asked for an approximate estimate, plus Bug/Improvement tickets for every gap.
6. **Second phase: an AI-assisted deep dive into the core flows.**
   - **I manually confirmed the AI-built knowledge base**, because the AI had become confused in places and needed correcting.
   - 📝 Have one concrete example of what it got wrong.
7. Produced the estimate and the backlog of tickets.

**Result**
- Expectations were reset using evidence rather than opinion.
- Scope was narrowed to the flows that mattered most.
- The backlog of gaps was transparent and estimated.
- 📝 Add the outcome: was work approved or delivered? How many tickets? How long did the investigation take compared with doing it without AI?

**Learning**
- AI dramatically sped up understanding a legacy system.
- But it **is only as good as the context you feed it, and it can confidently mislead**. Each AI-derived fact needed verification against the running system.
- The same lesson applies to tests: a green suite isn't proof. Validate that tests can actually fail.

**Why this story is strong for TRG:**
- It shows you giving AI context (an iterative knowledge base).
- It shows you evaluating AI critically (manual confirmation).
- It shows ownership (an evidence-based conversation with the PO).
- Spotting the false-positive tests shows your issue-spotting.

📝 **Concrete "AI was wrong" detail needed.** Pick one small, specific example. For instance:
- "It claimed the batch processor was invoked via X, but the request trace showed Y."
- "It listed a dependency version that didn't exist."
- "It described a Cancel flow that was actually dead code."

Specific beats general.

---

## Story B: Owned end to end (PIAD: Payment Integration Analytics Dashboard)

**Situation:** A new analytics dashboard (.NET 8 API, Vue 3 SPA, Kubernetes/Helm).
**Task:** As team lead, take it from requirements to production.

**Action**
- Contributed to the architectural definition. 📝 Name one concrete decision and its trade-off.
- Broke the work into stories and tasks, coordinated implementation and reviews, and made the technical decisions.
- Set up testing: WireMock for external dependencies, WebdriverIO for the UI.
- After deploying to the shared environment there was a **flood of customer feedback**.
  - I separated functional needs (columns, content) from cosmetic ones (styling).
  - We prioritised the functional changes and moved the cosmetic ones to a later iteration.
  - I managed stakeholders throughout.
- Supported the production deployment.

**Result:** 📝 delivered to production on date/schedule; usage; number of feedback items handled.
**Learning:** A usable product in customers' hands beats chasing perfection, and prioritisation has to be explicit and agreed.

---

## Story C: Unclear requirements (Quadriss: OCR and tagging platform)

**Situation:** Requirements were full of conflicting ideas and buzzwords. On top of that, US team members left and new managers joined.

**Task:** Turn vague ideas into an implementable architecture.

**Action**
- Converted the vague ideas into concrete technical requirements and asked clarifying questions.
- Built prototypes.
- Compared **Elasticsearch with Azure Cognitive Search**:
  - The source data was already JSON in Blob Storage.
  - Cognitive Search's native indexer avoided a custom ingestion pipeline, along with its retries, checkpointing and monitoring.
- Presented and defended the choice.
- Ran demos and technical sessions for the new managers.

**Result:** 📝 architecture accepted; delivery pipeline built; project kept moving.
**Learning:** When requirements are unclear, make them concrete with prototypes and explicit trade-offs, rather than waiting.

---

## Story D: Team changed how it worked (Robot Framework → C#/.NET test framework)

**Situation:** Automated tests were written in Robot Framework, separate from the application stack. Developers didn't own them.

**Task:** Improve maintainability and shared ownership of quality.

**Action**
- Ran sessions on the problems and on the alternatives in .NET and JavaScript.
- Built working API and UI test examples.
- Argued for using the same language as the application: IDE support, refactoring, developer–QA collaboration.
- Supported QA engineers through the migration with pairing and reviews.

**Result:** 📝 the team adopted the new framework; any measurable change (flakiness, time to write tests, developers contributing tests).
**Learning:** You change team practice with working examples and by lowering the cost of switching, not with slides.

**Alternative D2: changing how the team does code review.** Reviewers had to understand a change well enough to take it over if the author became unavailable. This increased shared ownership. It also fits TRG's AI theme: "reviewing AI code needs that same depth."

---

## Questions to ask them (pick 2–3)
1. How has the Activity Monitor team's workflow changed since adopting AI tools? What guardrails do you have for AI-generated code in reviews?
2. What does the Activity Monitor architecture look like: data volumes, ingestion, the frontend stack?
3. What would success look like for this role in the first 3–6 months?
4. How do you balance delivery speed from AI with code quality and test coverage?
5. What's the biggest technical challenge the team faces right now?
