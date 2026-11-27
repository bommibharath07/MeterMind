# Technical references and capability boundaries

Reviewed 2026-11-27. Package versions were checked against npm/NuGet registries and compiled locally. Supported does not mean all optional integrations have been exercised.

| Area | Primary reference | Implementation consequence |
| --- | --- | --- |
| Wirepas architecture | https://wirepas.freshdesk.com/support/solutions/articles/77000489804-wirepas-software-and-apis-overview | A gateway links backend MQTT to sinks; sinks are separate from gateways and mesh nodes. |
| Gateway API | https://developer.wirepas.com/support/solutions/articles/77000487992-how-to-communicate-with-a-wirepas-network-using-wirepas-gateway-api | No invented Wirepas topic, protobuf decoder or physical command. Canonical MeterMind MQTT ingestion is a separate schema. |
| Diagnostics | https://developer.wirepas.com/support/solutions/articles/77000561183-how-to-use-wirepas-mqtt-console- | Reported hop count, travel time and gateway/sink IDs need vendor mapping; optional SNR stays nullable. |
| DLMS/COSEM | https://www.dlms.com/core-specifications/ | Object identification, interface classes and communications are separate concerns. The simulator generates records, not DLMS APDUs. |
| IS 15959 | https://www.services.bis.gov.in/tmp/WCETD24727546_12022025_1.pdf | Companion requirements change through amendments. Licensed applicable parts, meter certificates, phase configuration and HES capture lists must be verified before compliance claims. |
| Indian deployment context | https://www.nsgm.gov.in/sites/default/files/AMISP_Contract_Final.pdf | Communication medium depends on deployment conditions and meter support. A cellular adapter contract does not establish hardware interoperability. |
| Angular compatibility | https://angular.dev/reference/versions | Angular 22.3.0, TypeScript 6.0.x, Node 22.23.5 and RxJS 7.8 are compatible. |
| EF PostgreSQL | https://www.npgsql.org/efcore/ | Npgsql EF provider 10.0.4 with EF Core 10.0.15. UTC timestamptz and parameterized queries. |
| Responses tools | https://developers.openai.com/api/docs/guides/function-calling | Preserve output items and function_call_output; use bounded tool loops. First turn requires a tool. Selected model tool support is checked by actual execution and the approved capability probe. |
| MCP transport | https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http | Official .NET SDK 2.4.0 stateless HTTP transport; body metadata, mirrored headers, bearer validation and utility claims are exercised in integration tests. |
| MQTTnet | https://github.com/dotnet/MQTTnet | MQTTnet 5.2.0.1603 TLS client; disabled until an explicitly configured canonical publisher is available. |
| ClosedXML | https://docs.closedxml.io/en/latest/ | Genuine XLSX generation with text-cell sanitization, tables, filters and units in column headings. |
| Azure extensions | https://learn.microsoft.com/azure/postgresql/extensions/concepts-extensions-considerations | TimescaleDB requires version compatibility, allowlisting, preload and edition review. Native PostgreSQL monthly partitions are the default. |
| Azure extension versions | https://learn.microsoft.com/azure/postgresql/extensions/concepts-extensions-versions | Recheck per server major/region before enabling optional extensions. PostGIS DDL is opt-in after verification. |
| Container Apps Jobs | https://learn.microsoft.com/azure/container-apps/jobs | Historical seeding and migrations run in private-network jobs, never API startup. |
| GitHub actions | https://github.com/actions/setup-dotnet and https://github.com/actions/setup-node | Requested v6 and v7 tags resolved to full commit SHAs; see action-pins.json. |

The available relevant MCP servers were Microsoft Learn, Figma and Playwright. No Figma file was supplied. GitHub, PostgreSQL, Context7 and OpenAI documentation MCP tools were not exposed; official documentation and local database tooling filled those gaps.

The OBIS entries are illustrative configurable examples. They are not a universal IS 15959 capture-object catalog. Electrical simulation assumes a balanced three-phase load. Firmware, packet quality and RF parameters are synthetic, not calibrated to Wirepas hardware. Antenna and congestion conclusions are hypotheses with alternatives. Rule-based risk is operational prioritization, not a statistically validated probability or predictive ML model.
