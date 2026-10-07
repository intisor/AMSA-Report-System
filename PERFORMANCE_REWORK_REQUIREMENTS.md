# AMSA Reporting System Performance Rework Requirements

## 1. Purpose and portfolio role

AMSA Reporting System is the portfolio's workflow and analytics application: a .NET 10 Blazor Web App that collects department reports, moves them through unit/state/national authorization, aggregates state results, stores attachments, and depends on AmsaAPI for identity and organization data. The rework must demonstrate resilient distributed integration, secure reporting workflows, efficient aggregation, and streaming file handling.

The rework is required because current hot paths combine broad EF entity graphs, in-memory aggregation, per-report downstream/context queries, circuit-scoped indefinite lookup caching, and fully buffered uploads. Polly options exist but are not wired into the typed client. No current latency, throughput, allocation, downstream-failure, attachment-memory, or database baseline is committed, so all performance changes are hypotheses until measured.

## 2. Verified current baseline

Verified implementation facts, not performance results:

- `AMSAReportingSystem/AMSAReportingSystem.csproj` targets `net10.0`, references EF Core SQL Server 10.0.5 and Polly 8.6.6, and references Client/Core/Infrastructure projects.
- `Program.cs` hosts interactive server and WebAssembly components, registers SQL Server `AddDbContext`, a typed `IAmsaApiClient`, scoped report/auth/directory services, singleton token cache, and local attachment storage.
- The app exposes minimal API routes for draft/report lifecycle, unit/state/national review, department/state attachment upload/download, and aggregation-backed views.
- Upload endpoints call `ReadFormAsync`, copy each file into `MemoryStream`, convert it to `byte[]`, pass it through services, then `LocalAttachmentStorage` writes all bytes. The configured service limit is 10 MB, so each concurrent upload can create multiple full-size buffers.
- Download uses `FileStream` through `Results.File`, so read streaming already has a viable abstraction (`AttachmentReadStream`). Storage is local filesystem and therefore not shared across instances.
- `UnifiedReportService.RecomputeStateAggregateAsync` loads all unit reports for a state/cycle, filters in memory, loads submitted department rows by ID list, removes/recreates auto-aggregated programs, and saves.
- `GetNationalReportsWithStateContextAsync` loads broad report graphs and then queries a state report, programs, and attachments inside a loop for every unit report, creating an N+1 query shape and repeated state-context retrieval.
- Several list/detail paths use multiple `Include`/`ThenInclude` chains and return entities or broad composites containing department JSON, attachments, and logs.
- `AMSAReportingDbContext` already defines useful unique and lookup indexes including report `(UnitId,CycleId)`, state report `(StateId,CycleId)`, status, cycle, state/unit, department `(ReportId,Department)`, attachments, programs, and activity logs.
- `AmsaApiClientOptions` declares retry and circuit-breaker settings, and the project references Polly, but `Program.cs` only calls `AddHttpClient`; no resilience pipeline is configured.
- The downstream client has a configured timeout (default documented as 300 seconds), catches broad exceptions, and reads complete response bodies as strings before deserialization.
- `AmsaTokenCache` is singleton and lock-protected, but token refresh has no single-flight guard, so concurrent expiry can trigger duplicate generation.
- `AmsaDirectoryLookupCache` is scoped per Blazor circuit and stores successes and fallback names indefinitely, without TTL, size limit, invalidation, stale metadata, or request coalescing.
- Authentication context, including the JWT token, is serialized into browser `sessionStorage`. Authorization for report operations is enforced in service methods through `ICurrentUserContext` and `ReportAccessService`, not only in UI components.
- `ReportAccessService` implements unit/state/national and department-scope checks. Integration fixtures currently grant a test user unit, state, national, and sudo access simultaneously, so they do not prove denial or tenant-boundary behavior.
- Unit tests exist for token/cache/auth/access/mappers. Integration tests use SQLite in-memory and a fake AmsaAPI client; current success tests cover active draft/history/detail/submit. SQLite cannot validate SQL Server plans, locking, collation, or provider-specific translation.
- The solution contains app, client, core, infrastructure, unit-test, and integration-test projects. No BenchmarkDotNet, k6, NBomber, OpenTelemetry, container, Aspire, or GitHub Actions workflow was found in the inspected source inventory.

## 3. Baseline and target outcomes

### Baseline first

Create deterministic small/medium/large reporting datasets with cycles, states, units, reports, all reportable departments, JSON payloads, activity logs, and attachments. Run at least three comparable trials and record SHA, SDK, host/container limits, SQL Server version/tier, AmsaAPI stub/real topology, data cardinalities, cache state, and network conditions.

Measure p50/p95/p99/max, RPS, errors/timeouts, SQL duration/roundtrips/rows, downstream calls, cache hit/miss/stale rates, attachment bytes and first-byte/complete time, allocations, GC, CPU, memory, connection pools, retry/circuit state, and authorization result. Establish separate cold-cache, warm-cache, healthy-downstream, degraded-downstream, and attachment profiles.

### Budgets after baseline

The accepted baseline is `B0`. Absolute SLOs are ratified only after usage/deployment discovery and stored in `performance-budgets.json` with workload/environment/owner/review date. Before that:

- correctness and authorization may not regress;
- errors may not exceed `B0` except injected failures;
- p95/p99, SQL roundtrips, downstream calls, and allocations may not regress beyond measured noise;
- claimed improvement must exceed variability while secondary metrics remain stable;
- no upload may buffer the entire file more than once in application memory, and the target design must stream with bounded buffers;
- degraded AmsaAPI behavior must be bounded by timeout/circuit policy and must not create retry storms.

## 4. Scope and non-goals

In scope: state/national aggregation, report list/detail projections, AmsaAPI resilience/token/lookup cache, attachment upload/download/storage, authorization at every query/mutation, SQL indexes/query plans/concurrency, observability, tests, load tools, CI, containers, and optional Aspire orchestration.

Non-goals: redesigning report forms or changing workflow policy; copying AmsaAPI's master organization data into this database without an approved ownership model; caching mutable report authorization decisions; adding distributed cache before a multi-instance/read-volume need is measured; replacing SQL Server or Blazor solely for speed; storing attachment binaries in the reporting tables.

## 5. Required architecture rework

### 5.1 Query and aggregation boundary

Introduce explicit query services and DTO projections for dashboard/list/detail use cases. Do not return broad EF graphs where a projection suffices. Every endpoint must declare included fields, paging/limits, sort order, cancellation, and authorization predicate.

Rewrite national-with-state-context as a bounded number of set-based queries (or one validated projection) keyed by state/cycle, then join in memory once. The number of SQL commands must not grow with report count.

Rework state aggregation so counts/sums/grouping execute in SQL where supported, without loading all reports and department entities. Preserve manual programs while replacing auto rows in one transaction. Define idempotency and concurrency: concurrent save/approve/recompute must not double rows, erase manual rows, or publish stale aggregates. Use an aggregate version or source watermark and record `LastAggregatedAt` only after successful commit.

Aggregation may be synchronous for measured small data or queued for larger workloads. If queued, expose 
 state (`pending/running/succeeded/failed`, source version) and use an outbox/idempotent worker; do not silently serve stale values as current.

### 5.2 AmsaAPI resilience and cache

Wire a .NET resilience handler/Polly pipeline into the typed client:

- per-attempt and total timeout for interactive calls;
- retries only for transient idempotent operations, with jitter and `Retry-After` support;
- no automatic retry for non-idempotent registration without idempotency support;
- circuit breaker telemetry by operation/status;
- cancellation propagation and bounded response reads;
- single-flight token refresh;
- no secrets/tokens in logs or traces.

Replace circuit-local dictionaries with a bounded `IMemoryCache` directory implementation; add distributed caching only when multi-instance evidence warrants it. Successes require TTL and invalidation/version semantics. Negative/fallback names require short TTL and explicit stale/fallback state. Coalesce concurrent misses. Stale-if-error is allowed for display names within a documented age, but authentication/authorization fails closed.

### 5.3 Attachment streaming

Change `IAttachmentStorage.SaveAsync` and service APIs from `byte[]` to `Stream` plus length. Use bounded multipart streaming, enforce body/section/file limits while copying, write asynchronously with bounded buffers, and remove partial files on cancellation/failure. Generate server-side names, normalize paths, prevent traversal/symlink escape, and save metadata only after durable content.

Define reconciliation for file-saved/database-failed and database-deleted/file-delete-failed cases. Local disk is a single-instance development option only; multi-instance/ephemeral production requires shared object storage such as Azure Blob. Add a malware scanning/quarantine integration point. Download remains streamed and may add ranges/conditionals if measured clients need them.

### 5.4 Authorization

Authorize before materialization and file access. Add policy-oriented endpoint filters/handlers so routes cannot omit checks. Projection queries include unit/state/national predicates rather than over-fetching then filtering.

Browser `sessionStorage` is not a secure token vault. Require a reviewed server-side session/BFF design with secure HttpOnly cookies or equivalent. Sudo must be time-bounded and audited and absent from normal performance fixtures.

## 6. Database, caching, and concurrency changes

- Capture SQL Server actual plans and Query Store evidence before changing indexes.
- Candidate compound indexes, subject to plans: reports `(StateId,CycleId,Status)` including unit/update fields; department reports `(CycleId,IsSubmitted,ReportId,Department)` including aggregate columns; activity `(ReportId,ActionAt)`; state programs `(StateReportId,IsAutoAggregated)`.
- Keep existing unique constraints and use optimistic concurrency/rowversion on workflow and aggregate roots where conflicting updates matter.
- Approval/rejection/submission must be atomic compare-and-set transitions; stale clients receive conflict, not last-write-wins.
- Add paging and bounded JSON/log/attachment lists. Avoid loading department JSON on summary boards.
- Do not cache report content or access decisions unless invalidation and authorization partitioning are proven.
- Startup migrations must be a deployment job before multiple web instances.

## 7. BenchmarkDotNet microbenchmarks

Create `AMSAReportingSystem.Benchmarks` only where CPU/allocation isolation is useful. Use .NET 10, `MemoryDiagnoser`, deterministic fixtures, and parameterized report/department/program counts. Cases:

- aggregate DTO construction from already materialized flat rows;
- old repeated grouping versus candidate single-pass grouping;
- national composite mapping from preloaded lookup dictionaries;
- authorization policy evaluation across representative role sets;
- JSON validation/parsing and summary projection for realistic payload sizes;
- cache key/result mapping and single-flight coordination.

Do not use BenchmarkDotNet to claim SQL, HTTP, or filesystem throughput. Those require SQL integration and load tests.

## 8. k6/NBomber/load-test plan

Use k6 for HTTP workflows and multipart/download traffic. Use NBomber if richer .NET orchestration, downstream fault injection, or direct telemetry is needed. Run through Kestrel with SQL Server and a controllable AmsaAPI stub; repeat critical runs with the real staging AmsaAPI.

### Data and workloads

Seed small/medium/large cardinalities for states, units per state, cycles, reports per cycle, all departments, JSON sizes, logs, and attachment sizes. Values are workload inputs and must be listed in each result; production gates are ratified after usage discovery.

1. Unit users load active cycle/draft, save departments, and submit.
2. Unit leadership lists/reviews/approves/rejects reports.
3. State leadership loads boards, recomputes/saves state aggregate, and submits.
4. National leadership loads the state-context board and acknowledges.
5. Cold/warm directory lookup with healthy AmsaAPI.
6. AmsaAPI latency, 429, 5xx, timeout, and outage to validate retry/circuit/stale behavior.
7. Concurrent uploads at boundary sizes, cancellations, invalid content, and download streaming/slow clients.
8. Concurrent edits/approvals/recompute against the same report to verify conflicts and idempotency.
9. Mixed realistic journey and soak for memory, pool, cache, and local/object-storage stability.

### Concurrency

Start smoke at one virtual user, then 5 and 10, then double staircase stages until the ratified expected concurrency and controlled saturation. Separate arrival-rate tests model deadline bursts. Attachment tests vary concurrent streams independently from report readers. Hold degraded-downstream stages long enough to observe breaker open/half-open/recovery without retry amplification.

### Metrics and assertions

Capture p50/p95/p99/max and RPS for each route/journey; error/timeout/conflict rates; SQL command count/duration/rows; AmsaAPI calls/retries/timeout/circuit state; cache hits/misses/stale/coalesced requests; time-to-first-byte and completion for files; bytes and allocation rate; GC/CPU/memory/thread pool; DB/HTTP connection pools; object-storage latency; and queue age if async aggregation is selected.

Assert returned rows are within actor scope, aggregate totals match an oracle, workflow transitions are legal, retries do not duplicate writes, streamed file hash/length matches source, and canceled uploads leave no reachable partial artifact.

## 9. OpenTelemetry

Instrument ASP.NET Core, HttpClient, EF Core/SqlClient, runtime/process, storage, cache, and custom workflow spans. Export OTLP. Required spans: `report.query`, `report.save`, `report.transition`, `state.aggregate`, `national.compose`, `amsaapi.request`, `amsaapi.token.refresh`, `directory.lookup`, `attachment.upload`, `attachment.download`, and reconciliation jobs.

Metrics: request p50/p95/p99 via backend histograms, SQL commands/rows/duration, aggregate input/output rows, downstream latency/status/retry/breaker, cache hit/miss/stale/size, token-refresh coalescing, upload/download bytes/duration/concurrency, authorization allow/deny by policy (not identity), conflicts, exceptions, allocations, GC, CPU, memory, and pools.

Use low-cardinality route/operation/status/scope labels. Never log report JSON, notes, file content, app secrets, JWTs, MKAN IDs, or names by default. Define redaction, retention, and access control.

## 10. Security and privacy

- Treat reports, notes, identities, and attachments as confidential organizational/member data; encrypt in transit/at rest and document retention/deletion/export.
- Require authenticated server-side authorization on every endpoint and attachment lookup; test object-level authorization/IDOR across unit/state/national boundaries.
- Enforce content length, extension plus content sniffing, safe filenames, quarantine/scanning, download disposition, and storage least privilege.
- Bound JSON depth/size and multipart sections to prevent resource exhaustion.
- Keep AmsaAPI app secret, SQL credentials, storage keys, and telemetry credentials in secret stores with rotation.
- Rate-limit login/token, save, transition, aggregation, and file operations by appropriate identity/scope.
- Do not retry unauthorized/forbidden requests or cache authorization failures as directory data.

## 11. Test requirements

- Unit tests for aggregate formulas, manual/auto program preservation, source versions, resilience classification, cache TTL/stale/coalescing, token single-flight, stream validation, and all allow/deny policy combinations.
- SQL Server container integration tests for provider translation, plans, rowversion conflicts, transactional workflow, aggregation idempotency, and migration compatibility.
- HTTP integration tests with least-privileged personas: department officer, unit leader, state leader, national leader, unauthenticated, wrong unit/state, and explicit sudo.
- Contract tests against AmsaAPI DTOs/routes/scopes and malformed/large response handling.
- Fault tests for AmsaAPI timeout/429/5xx, SQL deadlock/transient failure, storage failure, cancellation, and app restart during reconciliation.
- Attachment tests verify no full-memory buffering using allocation/working-set evidence and checksums.
- Existing SQLite tests remain fast functional tests but cannot be the only database gate.

## 12. GitHub Actions and regression strategy

Create workflows for .NET 10 restore/build, unit tests, SQLite tests, SQL Server container integration, contract tests, security/dependency scanning, container build, and artifact publication. Staging deploy requires successful checks and protected environments.

Performance policy:

- PR: compile benchmarks and run short correctness/load smoke; no statistical gate on shared runners.
- Nightly/manual/`performance` label/pre-release: full BDN and k6/NBomber on controlled infrastructure.
- Store raw result files, SQL plans, telemetry snapshot, environment manifest, and SHA.
- Derive noise from repeated `B0` runs; fail only when a practical threshold and measured variation are both exceeded in confirmation runs or consecutive nightlies.
- Correctness, cross-scope leakage, duplicate transitions, retry storm, or unbounded memory fails immediately.
- Compare cold/warm cache and healthy/degraded downstream separately; never average them into one misleading score.

## 13. Deployment, containers, and Aspire

Provide multi-stage non-root images, health/readiness, external configuration, graceful shutdown, migration job, and rollback. Production attachments must use shared durable storage when instances are replicated. Readiness distinguishes SQL, storage, and critical AmsaAPI dependencies; transient AmsaAPI degradation may expose degraded readiness/health according to the stale-data policy rather than crash-looping.

Aspire is justified for local/integration orchestration because this system spans Reporting, AmsaAPI, SQL Server, object-storage emulator, and OTLP tooling. Add it only if it makes contract/fault/load environments reproducible. Production deployment remains explicit and portable. Document connection pools, storage lifecycle, retry budgets, autoscaling, and downstream rate limits.

## 14. Phased backlog

### P0 — baseline, correctness, and safety

- Add CI, OpenTelemetry, deterministic seeds, and load harness.
- Capture `B0`; ratify data/concurrency profiles and budgets.
- Add denial-focused authorization/integration tests.
- Stream uploads end to end and secure storage paths/content.
- Wire bounded AmsaAPI timeout/retry/circuit behavior and single-flight token refresh.
- Remove N+1 national state-context retrieval.
- Add SQL Server tests and query-plan evidence.

### P1 — aggregation and efficient read paths

- Add DTO projections, paging, and summary/detail separation.
- Rework state aggregation into set-based transactional/idempotent processing with concurrency version.
- Implement bounded TTL/stale directory cache with miss coalescing.
- Add object-storage implementation and reconciliation.
- Add microbenchmarks where justified and full fault/soak coverage.
- Containerize and move migrations outside startup.

### P2 — operational/portfolio hardening

- Add Aspire orchestration if justified.
- Validate multiple instances, shared cache/storage, deadline burst, and downstream recovery.
- Add dashboards, alerts, runbooks, ADRs, absolute budgets, and case study.
- Tune only from traces/plans and revisit whether async aggregation/distributed caching is warranted.

## 15. Deliverables

- ADRs for aggregation consistency, AmsaAPI resilience/cache, attachment storage/streaming, and auth-session design.
- Benchmark project where justified; k6/NBomber scripts, seeds, AmsaAPI fault stub, and workload docs.
- SQL plans, baseline/post-change raw results, and versioned budgets.
- OpenTelemetry config, dashboards, alerts, and redaction rules.
- Expanded unit, SQL, authorization, contract, fault, storage, and load tests.
- CI, containers, migration workflow, optional Aspire topology, and operational runbooks.
- Portfolio case study with architecture diagrams and reproducible evidence.

## 16. Acceptance criteria

- National/state/unit queries execute a bounded SQL-command count independent of result count; the bound is asserted in tests.
- State aggregates match an independent oracle and remain correct under duplicate/retry/concurrent recompute; manual rows survive.
- The ratified workloads pass p50/p95/p99/RPS/error/allocation/resource/downstream-call budgets.
- AmsaAPI degradation is bounded by timeout/retry/circuit budgets, avoids retry amplification, and applies stale/fail-closed rules correctly.
- Uploads/downloads are streamed with bounded memory; checksum/length match; cancellation/failure leaves no reachable orphan.
- Every persona can access only permitted reports/files; cross-scope and unauthenticated tests fail closed.
- SQL Server integration validates translations, constraints, conflicts, migrations, and selected indexes/plans.
- Telemetry links request, authorization, SQL, downstream, cache, aggregation, and storage without sensitive payloads.
- Controlled CI publishes evidence and applies noise-aware gates; deployment supports shared storage, readiness, migration isolation, secrets, drain, and rollback.

## 17. Case-study evidence

Publish verified original constraints, hypotheses, dataset/environment manifest, before/after p50/p95/p99/RPS/errors/allocations/CPU/memory, SQL roundtrips/plans, AmsaAPI call/cache/retry charts, attachment memory/TTFB evidence, authorization matrix, aggregate reconciliation, and tradeoffs. Include at least one rejected optimization and separate microbenchmark, staging load, and production observations.

## 18. Risks and tradeoffs

- Set-based SQL improves roundtrips but can produce complex plans and schema coupling.
- Async aggregation smooths requests but introduces staleness, queues, and operations.
- Caching directory names improves resilience but can display stale organization data; it must never grant access.
- Retries can improve transient reliability or amplify outages; timeout/retry budgets are mandatory.
- Streaming lowers memory but complicates validation, scanning, and transaction coordination.
- Object storage enables scale-out but adds cost, eventual consistency considerations, and reconciliation.
- BFF/server-side sessions improve token protection but change hosting and authentication flows.
- SQLite speed is not SQL Server evidence; microbench
marks are not end-to-end capacity evidence.

## Definition of Done

The rework is done only when the agreed P0/P1/P2 scope is complete; all acceptance criteria pass on the documented reference topology; aggregation, resilience/cache, streaming, authorization, and SQL Server behavior are fault- and load-tested; telemetry and runbooks are operational; CI uses controlled noise-aware performance checks; deployment supports secure shared dependencies and rollback; raw baseline/post-change artifacts are retained; and the case study reports measured outcomes without invented metrics.
