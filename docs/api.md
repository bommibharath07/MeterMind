# HTTP API and MCP

All timestamps are ISO 8601 UTC. Date range inputs must be ordered and at most 366 days. JWT authentication is required for /api and /mcp. The claim utility_id is a UUID; sub/nameidentifier is the user; role claims use Administrator, NetworkEngineer, UtilityOperator, Technician or Viewer. The server derives utility scope. Supply `Authorization: Bearer <access-token>`.

| Route | Description |
| --- | --- |
| GET /health/live, /health/ready | Liveness; database connectivity and no pending migrations |
| GET /auth/config | Public identity provider configuration; local demo availability |
| POST /auth/demo | Local development only; short-lived demonstration token |
| GET /api/network/summary | Real database totals, states, thresholds, freshness and 24-hour trends |
| GET /api/meters | Search, RssiBelow, Status, SectorId, GatewayId, SinkId, MinHops, MinPdr, MaxLatencyMs, Firmware, Limit, Offset |
| GET /api/meters/{serial-or-uuid} | Meter details and freshness classification |
| GET /api/network/topology?at=… | Current topology or historical routing; historical health explicitly unknown |
| GET /api/meters/{id}/neighbors | Bounded-distance neighbor graph |
| GET /api/meters/{id}/routes?from=…&to=… | Routing history |
| GET /api/meters/{id}/profiles/{key}?from=…&to=…&limit=… | Reading/event profile; max 2,000 records per request |
| GET /api/meters/{id}/events?from=…&to=… | Event evidence |
| GET /api/meters/{id}/communication | Most recent 200 collection attempts |
| GET /api/meters/{id}/diagnosis | Deterministic findings, alternatives, recommended actions, heuristic priority |
| GET /api/network/affected/{uuid} | Potential downstream impact of a meter, sink or gateway failure |
| GET /api/gateways, /api/catalog | Gateway aggregates; configurable profile/OBIS/sector catalogs |
| GET /api/incidents | Utility incident list |
| PATCH /api/incidents/{uuid} | Operator JSON `{ "status": "investigating" }`; open/investigating/resolved |
| GET /api/work-orders | Proposed work orders |
| POST /api/work-orders/{uuid}/approve | Administrator review approval; no dispatch |
| POST /api/reports | Queue export; returns 202 and generated ID |
| GET /api/reports | Current user's report metadata |
| GET /api/reports/{uuid}/download | Authorized, complete, unexpired file |
| GET /api/simulation | Demo status and data clock |
| POST /api/simulation/start, stop, reset | Operators in the isolated demo utility only |
| POST /api/simulation/scenarios | `{ "meterSerial":"MM-0001", "cause":"weak-signal" }` |
| GET /api/settings, /api/audit | Current identity; administrator audit log |
| GET /api/copilot/status, /api/conversations | Configuration and owner-scoped conversation history |
| POST /api/copilot/chat | `{ "message":"…", "conversationId": null }`; SSE stream |
| /hubs/network | Authenticated SignalR; networkUpdated event grouped by utility |
| /mcp on McpServer | Authenticated stateless MCP Streamable HTTP; tools/list and tools/call |

Example report request:

```json
{"format":"xlsx","kind":"inventory","filter":{"rssiBelow":-90,"limit":1000}}
```

Formats: xlsx, csv, txt. Kinds: inventory, weak-signal, offline, diagnostics, profiles, incidents. Profile exports additionally require meterId, profile, from and to. Report readiness is polled through /api/reports. A download carries the same JWT as the original query; no public or guessed download URL is sufficient.

Profile keys: instantaneous, block, daily, billing, voltage-events, current-events, power-events, transaction-events, other-events, non-rollover-events. Seeded capture lists and OBIS objects are examples, not universal meter capabilities.

Controlled scenario causes: weak-signal, coverage-gap, antenna, congestion, routing-instability, gateway, firmware, power, dlms. Routing instability requires a connected node with a valid alternate parent; unsupported cases return 400. Ground truth is stored separately as a diagnostic record; the diagnostic rules do not read it. Reset restores baseline. Stop the worker while measuring scenario outcomes.

Copilot SSE events: delta {text}, tool {name,result,queryTime}, done, conversation {id}, error {message}. Tool output includes evidenceRef referencing ToolExecution. Stored conversation turns preserve recent context; each new question still requires a fresh database tool.

The 19 tools are listed in src/MeterMind.Infrastructure/ToolDispatcher.cs. MCP wrappers accept an argumentsJson object string using the same meterId, search, rssiBelow, status, sectorId, gatewayId, nodeId, profile, from, to, limit and description fields. Default tool limit is 50; maximum is 100. Unknown fields, tools, out-of-scope IDs and unrestricted SQL are rejected. propose_work_order creates only a proposal.

Failures use ProblemDetails (400 invalid input, 401 missing token, 403 forbidden, 404 absent or inaccessible resource, 409 state/configuration conflict, 429 rate limit). Detailed database errors remain server-side.

For MCP revision 2026-07-28, each request carries `_meta` with `io.modelcontextprotocol/protocolVersion`, `io.modelcontextprotocol/clientInfo` and `io.modelcontextprotocol/clientCapabilities`. Mirror the version in MCP-Protocol-Version, the method in Mcp-Method and the tool name in Mcp-Name for tools/call; Accept must include application/json and text/event-stream. These stateless requests do not use initialize or session IDs. See tests/MeterMind.Integration.Tests/McpTests.cs for an exercised example and the [official HTTP binding](https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http). Configure Mcp__AllowedOrigins as comma-separated exact browser origins; absent Origin is permitted for native clients, while unlisted browser origins receive 403.
