export interface Meter {
  id: string;
  serial: string;
  nodeAddress: number;
  sectorId: string;
  sinkId: string | null;
  nextHopId: string | null;
  hopCount: number | null;
  x: number;
  y: number;
  latitude: number;
  longitude: number;
  rssi: number | null;
  pdr: number | null;
  readSuccess: number | null;
  neighbors: number;
  firmware: string;
  lastCommunication: string | null;
  latencyMs: number | null;
}
export interface MeterView {
  meter: Meter;
  status: string;
  packetLoss: number | null;
}
export interface Sink {
  id: string;
  name: string;
  gatewayId: string;
  x: number;
  y: number;
}
export interface Topology {
  meters: MeterView[];
  sinks: Sink[];
  gateways: { id: string; name: string; latitude: number; longitude: number; online: boolean }[];
  neighbors: { meterId: string; neighborId: string }[];
  at: string;
}
export interface Summary {
  total: number;
  states: Record<string, number>;
  gateways: number;
  sinks: number;
  weakSignal: number;
  averagePdr: number;
  averageReadSuccess: number;
  openIncidents: number;
  mode: string;
  dataTime: string;
  queryTime: string;
  trend: { hour: number; rssi: number; pdr: number; readSuccess: number }[];
}
export interface Profile {
  timestamp: string;
  voltage: number | null;
  currentA: number | null;
  activePowerKw: number | null;
  cumulativeKwh: number | null;
  intervalKwh: number | null;
  quality: string;
  code?: string;
  description?: string;
}
export interface Report {
  id: string;
  kind: string;
  format: string;
  status: string;
  timestamp: string;
  expiresAt: string;
  error: string | null;
}
export interface Incident {
  id: string;
  title: string;
  severity: string;
  status: string;
  timestamp: string;
  meterId: string | null;
}
export interface Finding {
  rule: string;
  conclusion: string;
  evidenceQuality: string;
  evidence: string[];
  alternatives: string[];
  actions: string[];
}
