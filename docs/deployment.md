# Deployment and operations

## External setup

The private GitHub repository is connected. No Azure subscription, valid OpenAI key, production identity tenant or physical devices are configured. Local builds, database tests and template compilation are not deployment evidence. Container builds cannot be executed on the implementation host because Docker is unavailable.

1. Push this repository to your GitHub account. Protect main/master and configure GitHub Environments `staging`, `production` and `ai-evaluation`. Require reviewers for production and trusted live AI evaluation, and restrict deployment branches. These GitHub account settings cannot be enforced solely by YAML.
2. Establish federated GitHub OIDC identities for each environment. Grant only the required resource-group deployment privileges, managed-identity role-assignment rights, ACR push/import rights and cloud-job execution rights. Scope production separately. Do not create long-lived Azure client secrets.
3. Configure an identity provider issuing utility_id, subject and role claims. Register the API audience. Provide short-lived smoke-test access tokens for the demonstration utility. Administrative account and role assignments stay in that provider.
4. Configure environment variables: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP, AUTH_AUTHORITY, AUTH_AUDIENCE. Secrets: POSTGRES_PASSWORD and SMOKE_ACCESS_TOKEN. OPENAI_API_KEY and OPENAI_MODEL are optional until live AI evaluation.
5. Run `infrastructure.yml` on a protected branch to bootstrap each resource group. The Bicep template creates isolated VNet subnets, private PostgreSQL, a private Premium ACR, private Key Vault and Blob endpoints, managed identities, Container Apps environment, logs and Application Insights. Validate zone-redundant PostgreSQL availability and quotas in the selected production region.
6. Attach a managed self-hosted Linux runner with label `metermind-private` to the private network, or a peered management network with private DNS resolution. Install Docker/Buildx, Azure CLI/Bicep, GitHub CLI, Python, jq, PostgreSQL client utilities and the declared SDK/Node versions. Keep untrusted pull-request builds on hosted runners; never run them on this privileged runner. The initial infrastructure bootstrap can run on a hosted runner using ARM APIs if private runners have not yet been established.
7. Read bootstrap outputs and set ACR_NAME, ACR_SERVER, POSTGRES_SERVER and STORAGE_ACCOUNT. Production also needs SOURCE_ACR_RESOURCE_ID pointing to the tested staging registry. Grant the deployment principal cross-registry import/read rights. Trusted Azure service bypass is enabled for private registry import; confirm this policy is acceptable for your organization.

## Workflows

`ci.yml` performs locked .NET/npm restore, unit/diagnostic/mocked AI tests, isolated full PostgreSQL acceptance, idempotent SQL comparison, frontend build and Playwright. No OpenAI key is supplied to ordinary builds. `security.yml` checks dependency vulnerabilities and CodeQL. `simulation.yml` reuses CI for full historical acceptance.

`build-images.yml` waits for CI and creates immutable API, Worker, Simulator, MigrationJob, MCP and frontend images. Images receive SBOM/provenance and high-severity vulnerability scanning; the release artifact records exact digests and source SHA.

`deploy-staging.yml` requires a successful trusted image-build run for the current commit. The reusable deployment validates provenance, deploys jobs, waits for migration success, seeds the isolated demo, deploys services and runs authenticated readiness/database smoke tests. Only after those pass does it publish `staging-tested` evidence.

`deploy-production.yml` compares the exact release manifest against successful staging evidence for the same commit. It imports the tested images into the separate production registry and verifies every imported digest before deploying. Services receive production JWT settings, Key Vault secret references and managed identities. Migrations execute inside an ACA Job with private database access before service rollout. Production demo seeding is separate and explicit through `seed-database.yml`.

Migration, seed, infrastructure, rollout and rollback workflows share an environment concurrency group, preventing overlapping mutations. Production approvals and branch restrictions are GitHub Environment settings. API startup only checks readiness; it never applies migrations.

`rollback.yml` updates services to a previously compatible release's immutable image digests. Environment approval must verify schema compatibility. No automatic database downgrade or reversal of destructive migrations is performed. The rollback script resolves the target registry and imports an older digest from SOURCE_ACR_RESOURCE_ID when necessary, verifying the digest before updating services.

`backup-verification.yml` performs an Azure point-in-time restore to a uniquely named disposable server, queries the restored schema/data, creates a logical dump, verifies its archive listing and stores it in private Blob Storage. Cleanup deletes only that temporary restore. Actual restore execution, archive download and recovery-time objectives require cloud validation.

`ai-evaluation.yml` runs mocked tests and one trusted live tool-capability probe, capped at 300 output tokens, followed by a two-turn offline-meter conversation and exact XLSX row comparison. The conversation has at most three tool rounds and 1,000 output tokens per request; no live test runs on ordinary PR builds. Apply account/project spending limits in OpenAI as an additional usage control. This is a capability smoke test, not a full evaluation of root-cause accuracy or resistance to prompt injection.

## Database and extension operations

The baseline uses monthly native PostgreSQL partitions. Preserve `(UtilityId, MeterId, Timestamp)` and `(UtilityId, MeterId, Profile, Timestamp)` unique keys when changing storage. The migration creates partitions one year back and two years forward plus a default partition. Plan partition creation ahead of that horizon. Retention deletes utility-scoped old measurements; a shared database's partitions must not be dropped for one utility alone.

For PostGIS, inspect `pg_available_extensions`, allowlist the compatible extension for the exact Azure server version, then review and run infra/extensions.sql. Baseline latitude/longitude indexes work without PostGIS. TimescaleDB requires an independently reviewed migration and benchmark; compatibility and licensing/edition restrictions must be checked before provisioning it. Do not run both partitioning strategies on the same table without a deliberate migration.

Run migrations with the MigrationJob image in the private network. Commit EF migration source and generated idempotent SQL together. Existing compatible databases are tested by applying migrations twice; future data-shape changes require new upgrade fixtures and backups.

## Production acceptance still required

Execute all six container builds, CI workflows, infrastructure what-if and staging deployment in your account. Confirm network/DNS, ACR pulls/imports, Key Vault references, JWT role claims, TLS, report-worker throughput, Application Insights delivery and SignalR updates under representative load. Revalidate secrets and OIDC permissions without exposing them in logs.

Use the configured identity provider to obtain access tokens. The UI currently accepts an identity-provider bearer token through its connection/settings screen; a complete interactive OIDC redirect/refresh UX is an additional deployment integration. No passwords are stored in MeterMind.

Physical integration requires licensed DLMS/IS specifications, manufacturer OBIS/capture catalogs, HES credentials, gateway MQTT/protobuf mappings and hardware tests. The canonical MQTT schema is MeterMind-owned and must never be advertised as a verified vendor Gateway API decoder. Physical reboots, firmware updates, switching and dispatch remain absent from the exposed API.
