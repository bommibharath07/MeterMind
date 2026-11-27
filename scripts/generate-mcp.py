from pathlib import Path
names=['get_network_summary','search_meters','get_meter_details','get_weak_signal_meters','get_offline_meters','get_meter_neighbors','get_mesh_topology','get_routing_history','get_dlms_profile','get_meter_events','compare_meter_health','get_gateway_health','find_affected_nodes','diagnose_meter_failure','predict_network_risk','generate_excel_report','generate_csv_report','generate_text_report','propose_work_order']
text='''using System.ComponentModel;
using System.Text.Json;
using MeterMind.Application;
using ModelContextProtocol.Server;
namespace MeterMind.McpServer;
[McpServerToolType]
public sealed class MeterTools(IToolDispatcher dispatcher) {
'''
for name in names:
    text+=f'''[McpServerTool(Name="{name}"),Description("Utility-scoped {name}; JSON arguments use meterId, search, rssiBelow, from, to, profile, gatewayId, sectorId, limit, description. No utility ID or SQL allowed.")]
public Task<object> {''.join(s.capitalize() for s in name.split('_'))}([Description("Validated JSON argument object")] string argumentsJson="{{}}",CancellationToken ct=default)=>dispatcher.Execute("{name}",JsonSerializer.Deserialize<JsonElement>(argumentsJson),ct);
'''
text+='}'
Path('src/MeterMind.McpServer/MeterTools.cs').write_text(text,encoding='utf-8')
