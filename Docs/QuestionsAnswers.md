# Lead Software Engineer — Interview Examples

> A story bank of behavioural examples grouped by competency.

## Contents

1. [Teamwork](#1-teamwork)
2. [Problem Solving](#2-problem-solving)
3. [Communication](#3-communication)
4. [Leadership](#4-leadership)
5. [Customer Focus](#5-customer-focus)
6. [Adaptability](#6-adaptability)
7. [AI-Assisted Engineering](#7-ai-assisted-engineering)

### Project glossary

| Abbreviation | Project |
|---|---|
| **PIAD** | Payment Integration Analytics Dashboard |
| **CDX** | Medical Document Exchange Platform |
| **Quadriss** | Medical Document OCR and Tagging Platform (Azure Cognitive Search) |
| **Claims / Claims Suite / ClaimsService / ClaimsLog** | Medical Authorisation / Claims Platform |
| **RFMI, HealthNotebook, CSI, CoreDataService** | Other NantHealth projects |
| **Travis Perkins** | Distributed UK/Ukraine project (EPAM) |

---

## 1. Teamwork

### Working collaboratively with others

- **CDX – ActiveMQ:** Worked closely with another team to deliver functionality involving ActiveMQ management and successfully coordinated the release to production.
- **Claims Suite / Boston team:** Collaborated with the Boston development team when they wanted to start developing version 2 of the UI. We agreed on API endpoints and workflows, verified them first, and provided an agreed API contract so that their team could begin development independently.
- **Authorization project:** Coordinated work between teams, divided functionality into logical modules, and agreed which modules should be delivered first.
- **Travis Perkins:** Worked in a distributed project involving teams in the UK and Ukraine. We coordinated constantly through Team Lead meetings, cross-team discussions, and regular delivery planning.
- **RFMI / HealthNotebook:** Worked closely with other teams operating in related areas of the product to coordinate changes and avoid conflicts.

### Supporting colleagues

- **Claims project / India team:** Another team was asked to implement related functionality and requested knowledge transfer. I organised several sessions, demonstrated our CI/CD configuration, explained the automated functional tests, and described which functionality had already been implemented and verified.
- **Quadriss:** The project went through a difficult period when several members of the US team resigned or moved to other projects. New managers joined without much knowledge of the product. I helped organise demonstrations and technical sessions to explain the existing prototypes, challenges, and opportunities.
- **Placement students / interns:** Supported placement students across several projects, including RFMI and HealthNotebook. I reviewed their work, pair-programmed with them, helped them when they were blocked, and gradually gave them larger areas of responsibility.

### Managing different viewpoints

- **Travis Perkins:** We had multiple teams across the UK and Ukraine and frequently had different opinions about BDD, team structure, the role of QA, and coding standards. I participated in discussions, listened to competing views, and tried to reach decisions based on project needs rather than personal preferences.
- **HealthNotebook:** Discussed different approaches to automated testing and CI/CD and helped the team reach agreement on an approach that balanced coverage, maintainability, and delivery effort.
- **Authorization / Quadriss:** Worked through technical disagreements by focusing discussions on requirements, risks, maintainability, and delivery impact.

---

## 2. Problem Solving

### Identifying issues

- **Security Questions:** While implementing configuration to make security questions optional, I noticed an unrelated defect where the second security question could be stored with an incorrect or duplicate name. I raised and investigated the problem rather than ignoring it because it was outside the original task.
- **Functional testing:** Identified situations where exceptions were being swallowed by the automated test framework, meaning tests could report success even though part of the application flow had failed.
- **GZIP / CDX:** While creating performance tests for endpoints processing large JSON metadata payloads, I noticed that HTTP compression had a significant impact on performance and investigated it further.
- **Pendo:** Nobody on the development team had access to or experience with Pendo. I contacted the Product Manager, requested the required permissions, and raised access requests for the shared environments so that development could proceed.
- **JSON serialization:** Noticed developers continuing to introduce Newtonsoft.Json into newer .NET projects despite System.Text.Json being available natively in modern .NET, and raised the opportunity to standardise on the newer library where appropriate.

### Analysing information

- **Quadriss:** Requirements initially contained many conflicting ideas, buzzwords, and loosely defined expectations. I helped turn these into concrete technical requirements, investigated possible technologies, and compared options before proposing an architecture.
- **Helm Tests:** Kubernetes was relatively new within the company and our team was one of the early adopters. Because of my QA automation background, I was asked to investigate `helm test`, understand how it worked, create an example, and recommend how we should use it.
- **GZIP performance investigation:** CDX had performance problems with endpoints transferring large JSON metadata files. I created a JMeter performance suite, established performance baselines in an integration environment, and compared different configurations. I discovered that HTTP compression significantly affected the results and investigated browser and web-server support before recommending the change.
- **Gradle:** Investigated build configuration and behaviour to understand and resolve problems rather than treating build failures as isolated issues.

### Implementing solutions

- **End-to-end delivery:** Took projects from Architectural Definition / requirements through implementation to production, including breaking requirements into user stories and tasks, managing changes, reporting progress, coordinating with other teams, and supporting deployment.
- **RFMI:** Took responsibility for delivery while a senior engineer was away. I worked with two placement students, coordinated with another team working in the same Authorization area, reviewed the implementation, and helped deliver the functionality.
- **PIAD:** Took the project from Architectural Definition, broke the work into user stories and tasks, coordinated implementation and reviews, made technical decisions, managed stakeholders, and supported deployment.
- **Claims:** Investigated existing functionality and automated coverage, identified gaps, updated configuration and tests, and managed stakeholder expectations regarding what was actually covered.
- **CDX:** Took responsibility for completing development work while the Team Lead / Principal Engineer was on annual leave.
- **CSI:** Delivered enhancements and bug fixes to an older legacy application while balancing maintainability with the limitations of the existing system.
- **CoreDataService:** Supported migration to .NET Core and Kubernetes. Fixed and migrated tests to the newer framework and used automated testing to verify that functionality continued to work correctly.
- **Test framework migration:** Created examples and helped convince the team to move to a modern .NET-based automation framework.
- **Quadriss Search:** Investigated Lucene/Elasticsearch versus Azure Cognitive Search and helped select the solution that best matched our requirements.
- **CI/CD:** Designed and implemented CI/CD capability for a new area of the organisation.
- **Commons Library:** Created reusable shared functionality that could be consumed by multiple applications.

---

## 3. Communication

### Influencing stakeholders

- **Scope changes mid-iteration:** When stakeholders requested additional functionality during an iteration, I explained the impact on existing commitments and asked them to prioritise the new work against tasks already in progress rather than simply adding more scope.
- **PIAD:** After deploying to a shared environment, we received a large amount of customer feedback. I helped separate genuine functional requirements from cosmetic improvements. We prioritised important column/content changes and postponed lower-priority styling and UI adjustments to a later iteration.
- **Claims:** Demonstrated that existing automated test coverage was giving false confidence by providing concrete examples where tests passed despite functionality being broken. This helped the team agree on where testing investment was required.
- **ClaimsLog:** An architect believed that an existing cancellation flag was no longer used elsewhere. I investigated the application and performed exploratory testing, demonstrating that the flag was still involved in several workflows. I also highlighted that a ClaimsLog V2 redesign was already on the roadmap and questioned whether significant investment in the legacy implementation represented good value.

### Presenting information

- **Robot Framework migration:** I organised several sessions explaining problems with our existing Robot Framework approach and presented alternative .NET and JavaScript testing frameworks. I explained the benefits of using the same language and tooling as the application, including IDE support, refactoring, maintainability, and better collaboration between developers and QA. I then created working API and UI test examples to demonstrate the approach.
- **Quadriss – Cognitive Search vs Elasticsearch:** Presented and defended our technology choice. Our source data was already stored as JSON in Azure Blob Storage. Azure Cognitive Search provided a native indexer that could read and periodically index the data directly, whereas Elasticsearch would have required us to introduce and maintain an additional ingestion mechanism. I explained the trade-offs around operational complexity, retries, checkpointing, monitoring, and maintenance.
- **Introducing test automation:** There was initially resistance because manual testing was the established approach. I presented the benefits of repeatability and regression coverage, used examples of defects that automation could have caught, and proposed a small pilot. The pilot detected several real defects, which helped gain management support for broader automation adoption.

### Handling difficult conversations

- **Placement student performance:** A placement student was repeatedly asking multiple team members to help with the same task and then reporting the task as complete. I arranged a private conversation and explained that their placement performance was judged not only by completed tasks but also by their development, independence, and ability to take ownership. We agreed on clearer expectations and how they should ask for support.
- **QA Automation Engineer performance:** An engineer was taking substantially longer than expected to complete assigned tasks and was not raising blockers or asking for help. I arranged a one-to-one conversation, asked whether there were technical or personal issues affecting them, explained the expected duration based on comparable work, and made it clear that their current performance needed to improve.
- **BDD / Travis Perkins:** As Acting Team Lead on a large distributed project, I disagreed with the QA Manager's push toward BDD. Rather than simply rejecting the proposal, I reviewed the material they provided, researched the advantages and disadvantages myself, and suggested that we first confirm that Business Analysts and Product Managers would actively participate. My concern was that BDD implemented only by QA could create an additional maintenance layer without delivering the main collaborative benefits of BDD.
- **Embedded QA:** Advocated for embedding QA engineers within development teams because it allowed earlier involvement in requirements, improved testability, better agreement on unit/API/UI responsibilities, faster defect discovery, and stronger shared ownership of quality.

---

## 4. Leadership

### Taking initiative

- **Claims / Batch Processing:** While preparing a new version of ClaimsService, I contacted the Product Manager to clarify whether batch or regular processing was more important. After learning that customers preferred batch processing, I independently investigated the existing implementation, configured it correctly, and demonstrated that Batch Processing worked with version 1. I then investigated support for version 2 and identified that an old internal library was incompatible, allowing us to make an informed decision about whether to update the library or change Claims Suite.
- **Helm Tests:** Took ownership of investigating Helm tests when Kubernetes adoption was still new within the organisation.
- **Acting Team Lead:** Frequently stepped in as Acting Team Lead when the Team Lead was unavailable, including leading meetings, clarifying requirements, coordinating delivery, reviewing work, and managing handovers.
- **Quadriss:** Took initiative when requirements were unclear by investigating technologies, producing prototypes, and helping convert broad ideas into implementable solutions.
- **CI/CD:** Identified opportunities to automate deployment/build processes rather than continuing with manual processes.

### Supporting and mentoring others

- Mentored placement students and interns, giving them progressively more challenging tasks and helping them move toward independent delivery.
- **CoreDataService:** Supported engineers through migration work, testing changes, and adoption of newer technologies.
- **Claims:** Helped developers and QA engineers understand existing functionality and improve automated coverage.
- Used my previous QA Automation experience to help developers understand testing strategy and QA engineers understand development practices.
- Supported QA engineers when migrating from Robot Framework to a C#-based automation framework through examples, code reviews, pairing, and technical guidance.
- Improved the code review process by encouraging reviewers to understand the implementation deeply enough that they could take ownership of the work if the original developer became unavailable.

### Driving improvements

- Improved the code review process to increase shared ownership and knowledge transfer.
- Improved MSBuild / build processes to make builds more reliable and maintainable.
- Introduced clearer Git commit / pull-request naming conventions.
- Advocated for migration from Robot Framework to a modern programming-language-based test framework.
- Helped introduce and expand automated testing where manual testing had previously been the standard approach.
- Introduced / supported the use of **Sealed Secrets** to improve secure secret management in Kubernetes and Git-based deployment workflows.

---

## 5. Customer Focus

### Delivering excellent service

- **PIAD:** We received a large number of additional requests and changes. I focused the team on delivering the highest-value functionality first and explained that having a usable, slightly imperfect product in the customer's hands was more valuable than indefinitely delaying delivery while chasing perfection.
- **Quadriss:** Despite unclear requirements, management changes, and several team members leaving the project, I remained focused on producing something usable. We developed prototypes, established the technical architecture, created the delivery pipeline, and continued moving the project forward.

### Managing expectations

- **Claims Submission:** Stakeholders had expectations that did not match the actual state of the application and test coverage. I provided concrete examples, explained the limitations, and made sure future commitments were based on what the system could realistically support.
- **PIAD:** Managed a large number of late change requests by explaining their impact, asking stakeholders to prioritise them, and moving lower-value changes into later iterations rather than promising that everything could be delivered immediately.

### Resolving complaints

I do not have a particularly strong direct customer complaint example. My normal approach would be:

1. Understand exactly what the person is unhappy about.
2. Establish whether the cause is a defect, misunderstanding, missed expectation, or communication issue.
3. Acknowledge the concern rather than immediately defending the team.
4. Agree on the appropriate resolution and communicate progress.
5. Analyse why the issue happened and introduce an improvement if appropriate so that the same complaint does not recur.

---

## 6. Adaptability

### Managing change

Managing change means understanding the impact quickly, communicating it clearly, reprioritising where necessary, and helping the team remain focused.

- **PIAD:** Requirements and priorities changed significantly after customers started using the version deployed to the shared environment. I helped break new feedback into priorities, protected the most important delivery objectives, and moved lower-priority changes into later iterations.
- **Quadriss:** Adapted to major changes in project direction, management, team membership, and requirements while continuing to move the technical solution forward.
- **Technology migration:** Adapted to the company's move toward Kubernetes, Helm, .NET Core, and new deployment approaches while continuing to support existing systems.

### Learning new skills

- **Kubernetes / Helm:** Learned Kubernetes and Helm while the organisation was beginning its adoption. Applied that knowledge directly to deployments, troubleshooting, Helm tests, secrets, and application migration.
- **Quadriss:** Learned and evaluated unfamiliar technologies including Azure Cognitive Search, Elasticsearch, and related search/indexing concepts in order to make architectural recommendations.
- **C# / Development:** Successfully transitioned from a strong QA Automation background into software development and then into senior and leadership responsibilities.

### Working under pressure

When working under pressure, I focus on prioritisation and communication. I separate what is genuinely urgent from what is simply creating noise, make sure the team concentrates on the highest-impact problem, and keep stakeholders informed so engineers can focus on resolving the issue.

- Taking over delivery responsibilities when the Team Lead or Principal Engineer was unexpectedly unavailable.
- Resolving deployment or environment issues close to an important release.
- **Quadriss:** Continuing delivery despite major team changes and uncertainty around the project's future.
- **PIAD:** Managing a flood of customer change requests close to delivery while protecting the core release.

---

## 7. AI-Assisted Engineering

### Using AI to revive a legacy project (AI helped + AI was wrong + ownership)

- **Claims project revival:** A Claims project had been on hold for about 4 years. It had been deployed only to a shared UAT environment and never to production. Product management and the engineering manager expected it to work well. It didn't:
  - internal projects such as the **batch processor** had been updated in the meantime
  - several **libraries were outdated**
  - the **functional tests were false positives**, marking flows as passed despite errors
- **How I used AI:**
  - I built an AI-assisted knowledge base stage by stage through many focused prompts.
  - AI helped me identify dependencies and get the server running locally.
  - I **validated its findings by sending real requests to the locally running server**.
- **Evidence and stakeholders:**
  - I collected the dependencies, reproductions and facts.
  - I proposed to the Product Owner that we focus on the core flows: **Submit claim, Cancel claim, Adjust/Repair**.
  - They agreed and asked for an approximate estimate plus Bug/Improvement tickets for the gaps.
- **Second phase:**
  - An AI-assisted deep dive into the core flows, with **manual confirmation that the knowledge base was correct**.
  - The AI got confused in places and needed many corrections.
  - Then I produced the estimate.
- **Learning:**
  - AI accelerated understanding of an unfamiliar legacy system.
  - But every AI-derived fact had to be verified against the running system.
  - Similarly, green tests had to be checked to confirm they could actually fail.

> Full STAR version: [`../KnowledgeBase/08-behavioural-stories.md`](../KnowledgeBase/08-behavioural-stories.md)
