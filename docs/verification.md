# Verification record

Implementation environment: Windows 11 (10.0.29708), 12 logical processors, .NET SDK 10.0.404 / runtime 10.0.15, Node 22.23.5 and isolated native PostgreSQL 18.7. Local data files are under .local; .tools contains development-only SDK/database binaries. Docker is unavailable.

## Verified locally

- .NET solution builds: Domain, Application, Persistence, Infrastructure, API, Simulator, Worker, AI, MCP, Reporting and MigrationJob.
- Unit tests cover exactly 300 unique meters, radius bounds, bounded radio edges, deterministic coordinates/routes, loop-free sink termination, stale/missing states, integrated interval/cumulative energy and spreadsheet formula injection.
- Nine controlled rule fixtures, a healthy control and an insufficient-evidence control exercise diagnostics without access to ground-truth labels in the rules.
- Mocked Responses tests verify required database tools, function output continuation, actual SSE deltas, explicit missing credentials and rejection of SQL/utility override arguments.
- The committed SQL artifact applied twice in a fresh disposable schema; fresh PostgreSQL migrations succeeded. Compatible existing migrations applied repeatedly. Full default seed produced exactly 3,897,300 baseline records and 300 meters. Repeated seeding preserves counts and now short-circuits via fresh seed-completion state.
- Database acceptance covers all ten profile modules, 18 matching weak-signal meters, exact matching Excel serials, cross-utility detail/export denial, unauthorized REST access and readiness.
- MCP acceptance exercises anonymous rejection, untrusted Origin rejection, tools/list and another provisioned utility's empty search result through the official SDK. Modern requests use the 2026-07-28 per-request metadata and mirrored HTTP headers.
- In total: 26 backend tests pass (9 unit, 11 diagnostic, 3 mocked AI and 3 integration). Four Playwright tests pass, including asynchronous workbook download and logical renderer/filter changes.
- Three frontend API tests and an Angular production build pass after clean npm ci.
- Playwright tests pass for all 300 map nodes, database RSSI filtering, meter diagnostics, ten profile tabs, explicit OpenAI setup state and responsive layout.
- NuGet vulnerability audit reports no high/critical findings; npm audit reports zero vulnerabilities.
- All 12 requested workflows and reusable workflows parse. Official actionlint validates expressions and configured runner labels. Bash syntax checks pass. External ShellCheck/Pyflakes were not installed on this Windows host.
- Bicep compiles locally. No Azure resources were created and no GitHub workflow run is claimed.

## Performance targets and local measurements

Proposed staging targets on a minimum 2-vCPU PostgreSQL server and 1-vCPU API with the complete default dataset: warm P95 summary <1,000 ms, filtered inventory <250 ms, topology <500 ms; first 300-node local map display <3 seconds on a supported desktop browser. These are acceptance targets, not hardware-independent guarantees.

Ten sequential warm local HTTP samples per route (one warm-up excluded) produced:

| Query | Median | P95 nearest rank |
| --- | ---: | ---: |
| 24-hour summary | 125 ms | 190 ms |
| RSSI below -90 dBm | 17 ms | 28 ms |
| Full topology | 51 ms | 99 ms |

The database held 3,897,300 baseline records. These are single-client loopback measurements on the development machine, not concurrent-load, cold-cache or Azure benchmarks. scripts/benchmark.py reproduces the measurement and writes artifacts/performance.json. Cold historical seed duration was not instrumented; the complete command succeeded after several minutes. Warm idempotent seed checks measured approximately 0.5 seconds and must not be reported as cold generation speed.

The initial frontend bundle is approximately 544 kB raw / 124 kB estimated compressed. Mapping, graph and chart engines are loaded lazily; their additional chunks are larger. Budgets explicitly account for Angular Material, accessibility, map styles and the NOC application shell.

## Fixes found during verification

- Windows build locks: stop a running Debug API before replacing its assemblies.
- SignalR bearer query handling and hub utility context were corrected after real browser failures.
- Seed-completion tracking was corrected so repeat seeding does not reattempt millions of conflict inserts.
- Reports now reject an unprovisioned utility with a clear state error.
- Generic worker startup excludes HTTP-only authorization services; the persistent local worker starts successfully.
- Excel column sizing avoids desktop font dependencies inside Linux containers.
- MCP requests were brought into conformance with the current SDK's stateless protocol metadata and HTTP header requirements.

## Remaining external acceptance gates

Docker image builds/scans, actual GitHub workflow execution, Azure private DNS and identity setup, staging deployment/smoke tests, exact production promotion, backup recovery execution and concurrent cloud performance remain unverified. Interactive production OIDC login/refresh requires identity-provider integration; current production access accepts valid bearer tokens.

No live OpenAI call was made. Model capability, live tool correctness, adversarial prompt-injection behavior and narrative factuality need approved evaluations using your configured model. The application constrains tool access and exposes evidence; no finite test establishes that an LLM can never hallucinate.

No physical meter was accessed. Licensed applicable IS/DLMS specifications, manufacturer mappings, gateways, HES credentials and radio measurements are required before interoperability or regulatory claims. PostGIS enrichment and a Timescale-specific storage migration are optional and have not been executed here.
