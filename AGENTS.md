# MeterMind contributor guidance

Use .NET 10 and Angular with strict TypeScript. Keep dependency lock files committed.
Domain must not depend on infrastructure. REST, AI and MCP must reuse application services.
Every query, export and mutation must be scoped to the authenticated utility. Do not accept utility identity from tool arguments.
Never run production migrations at API startup. Use the migration executable/job.
Keep synthetic data labeled. Never claim physical Wirepas/DLMS compatibility or predictive ML validation from simulator tests.
Run `dotnet test MeterMind.slnx` and `npm ci && npm run build` in frontend after relevant changes.
Use isolated test databases. Never reset an externally supplied database.
Keep PROGRESS.md current with exact commands, results and unmet acceptance gates.
