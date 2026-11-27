# MeterMind

An Angular and .NET 10 network operations application backed by PostgreSQL, with a deterministic 300-meter RF mesh simulator, DLMS profile explorer, diagnostics, genuine reports, Responses API Copilot and authenticated MCP tools.

**Synthetic data is prominently labeled. No physical Wirepas, DLMS or cellular interoperability is claimed.** Azure deployment and live OpenAI evaluation require your accounts and configuration. See [PROGRESS.md](PROGRESS.md) for actual verification and remaining acceptance gates.

## Run with Docker

Requires Docker Engine/Desktop with Compose, Node 22.23.5 for local frontend development and .NET SDK 10.0.404 for source builds.

```powershell
Copy-Item .env.example .env
# Set POSTGRES_PASSWORD to a local random password in .env.
docker compose up --build
```

Open http://localhost:4200. Compose starts PostgreSQL, runs dedicated migrations, seeds the full history once, then starts API, Worker, MCP and frontend. First seeding may take several minutes. Repeat starts reuse persistent PostgreSQL data. The UI opens a local development session automatically. The MCP service uses an actual configured JWT identity provider, not the API's ephemeral demo signing key.

## Run natively on this Windows workspace

Docker is not installed in the implementation environment. A reproducible local bootstrap keeps the SDK and isolated PostgreSQL tool binaries in ignored .tools; database files remain in ignored .local. It does not modify your existing PostgreSQL databases.

```powershell
./scripts/bootstrap-local.ps1
```

Terminal 1:

```powershell
node scripts/local-postgres.mjs
```

Terminal 2 (the first seed creates 3,897,300 baseline rows):

```powershell
. ./scripts/local-env.ps1
./.tools/dotnet/dotnet.exe run --project src/MeterMind.MigrationJob
./.tools/dotnet/dotnet.exe run --project src/MeterMind.Simulator
./.tools/dotnet/dotnet.exe run --project src/MeterMind.API --urls http://127.0.0.1:5080
```

Terminal 3:

```powershell
npm --prefix frontend start -- --host 127.0.0.1
```

Terminal 4 (needed for continuous simulation; the development API processes local report jobs itself):

```powershell
. ./scripts/local-env.ps1
./.tools/dotnet/dotnet.exe run --project src/MeterMind.Worker
```

Stop a foreground process with Ctrl+C. Native Windows rebuilds can fail while the development API holds loaded assemblies; stop it before rebuilding, or build/test in Release while serving Debug. Do not run two PostgreSQL launchers against the same data directory.

## Configure

Use environment variables; keep secrets server-side. Production authentication requires Auth__Authority and Auth__Audience, with utility_id, subject and role claims. All cloud deployments force DemoAuth=false.

| Variable | Purpose |
| --- | --- |
| ConnectionStrings__MeterDb | PostgreSQL connection string; cloud requires verified TLS |
| OpenAI__ApiKey, OpenAI__Model | Responses credentials and a model supporting function tools |
| Simulation__Enabled | Enables controls only for the fixed isolated demo utility |
| Simulation__Latitude, Longitude, RadiusM, Seed, Days, End | Historical seed configuration; defaults Bengaluru, 1,000 m, 20261127, 30 days ending 2026-11-27T00:00:00Z |
| Health__WeakRssi, MinPdr, StaleMinutes, OfflineMinutes | Configurable state thresholds; defaults -90, .9, 15, 60 |
| WorkerUtility | Provisioned utility UUID for a worker; default demo utility |
| RetentionDays | Worker measurement retention, default 90 days relative to utility clock |
| APPLICATIONINSIGHTS_CONNECTION_STRING | Azure Monitor OpenTelemetry destination |
| OTEL_EXPORTER_OTLP_ENDPOINT | Optional OpenTelemetry collector endpoint |
| Mqtt__Enabled, Host, Port, Topic, Username, Password | Explicit canonical MQTT ingestion, TLS required; separate non-simulation worker |

End, center and seed are deterministic inputs. A different historical configuration requires an explicit isolated demo reset. Reset deletes only demo-utility data, preserves the utility record and reseeds. Stop continuous simulation while evaluating controlled scenarios.

OpenAI is optional. Leave `OPENAI_API_KEY=` and `OPENAI_MODEL=` blank in `.env` (or `OpenAI__ApiKey` and `OpenAI__Model` for native development). Blank placeholders also appear in the API's appsettings.json. MeterMind runs normally: inventory, maps, profiles, diagnostics, simulation and Excel exports do not require an OpenAI key. Copilot shows “AI chat is paused” with links to meters and reports. Chat submissions return a clear 503 response before creating a conversation or contacting OpenAI. Keep the live AI workflow unused until you choose to configure a valid key later.

No canned answers or invented meter results are supplied. When AI is connected, the selected model's tool capability is checked during execution; the approved AI workflow adds a bounded capability probe. A full live factuality evaluation remains required before operational adoption.

## Verify

```powershell
$env:METERMIND_TEST_CONNECTION='Host=127.0.0.1;Port=55432;Database=metermind_test;Username=metermind;Password=local-only'
$env:METERMIND_FULL_SEED='1'
./.tools/dotnet/dotnet.exe test MeterMind.slnx
npm --prefix frontend ci
npm --prefix frontend test
npm --prefix frontend run build
# With API and frontend running:
npm --prefix frontend run test:e2e
```

Without METERMIND_TEST_CONNECTION, database tests explicitly skip. They require a database name ending in _test. A fast database test can omit METERMIND_FULL_SEED and use one day in a separate clean test database; do not switch durations against an existing seed. Ordinary tests mock OpenAI HTTP responses and incur no API usage.

The default record count is 300 × (8,640 telemetry + 2,880 instantaneous + 1,440 block + 30 daily + 1 billing) = **3,897,300**, plus events, routes, attempts and operational records. Measurement keys, constraints, partitioning and idempotent migration SQL are committed.

## Repository guide

- [Architecture and boundaries](docs/architecture.md)
- [REST, tools and MCP](docs/api.md)
- [Deployment and GitHub setup](docs/deployment.md)
- [Verification and performance](docs/verification.md)
- [Official technical references](docs/technical-references.md)
- [Idempotent SQL artifact](docs/sql/migrations.sql)

The 12 requested GitHub workflows and two reusable workflows are in .github/workflows. Remote actions are pinned to verified full commit SHAs. CI uses locked restores, isolated PostgreSQL, mocked AI and browser checks. Cloud jobs use GitHub OIDC and a private-network runner; production promotion compares the exact staging-tested manifest and preserves image digests. No cloud success is implied by local template validation.

## Live Copilot acceptance

Save a valid key as the repository Actions secret OPENAI_API_KEY. Set the OPENAI_MODEL repository variable if you want to override gpt-4.1-mini. Run the Trusted live AI evaluation workflow on main: it lists actual offline meters, follows up with an Excel request, and compares every workbook serial with PostgreSQL. A successful run uploads offline-meters.xlsx and result.json. This manual workflow performs bounded, billable OpenAI calls; ordinary CI does not.

For a local session, run python scripts/openai-session.py and enter the key at its hidden prompt. The helper validates the credential and holds it only in the API process environment. GitHub cannot reveal a saved Actions secret to the local application.
