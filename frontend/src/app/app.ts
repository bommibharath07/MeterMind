import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Api } from './api';
import { Network } from './network';
import { Chart } from './chart';
import { Meter, MeterView, Topology, Summary, Profile, Report, Incident, Finding } from './types';
import { Evidence } from './evidence';
import { A11yModule } from '@angular/cdk/a11y';
@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule, RouterLink, MatButtonModule, Network, Chart, Evidence,A11yModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit, OnDestroy {
  api = inject(Api);
  router = inject(Router);
  view = signal('overview');
  collapsed = signal(false);
  loading = signal(true);
  error = signal('');
  notice = signal('');
  connected = signal(false);
  summary = signal<Summary | null>(null);
  topology = signal<Topology | null>(null);
  meters = signal<MeterView[]>([]);
  reports = signal<Report[]>([]);
  incidents = signal<Incident[]>([]);
  catalog = signal<{
    profiles: { key: string; name: string }[];
    sectors: { id: string; name: string }[];
  }>({ profiles: [], sectors: [] });
  gateways = signal<
    {
      gateway: { id: string; name: string; online: boolean };
      meters: number;
      readSuccess: number;
    }[]
  >([]);
  selected = signal<Meter | null>(null);
  profileRows = signal<Profile[]>([]);
  findings = signal<Finding[]>([]);
  simulation = signal<{ running: boolean; clock: string; seed: number; start: string } | null>(
    null,
  );
  messages = signal<{ role: string; text: string; tools: { name: string; result: unknown }[] }[]>(
    [],
  );
  sending = signal(false);
  aiConfigured = signal(false);
  conversationId: string | undefined;
  private abort: AbortController | null = null;
  query = '';
  status = '';
  sector = '';
  rssi = '';
  profile = 'block';
  meterSerial = 'MM-0012';
  from = '';
  to = '';
  prompt = '';
  tokenInput = '';
  playback = '';
  workOrders = signal<
    { id: string; description: string; status: string; humanApproved: boolean }[]
  >([]);
  settings = signal<{
    utilityId: string;
    userId: string;
    canOperate: boolean;
    roles: string[];
  } | null>(null);
  scenarioCause = 'weak-signal';
  scenarioMeter = 'MM-0001';
  neighborRows = signal<{ neighborId: string; distanceM: number; rssi: number }[]>([]);
  routeRows = signal<{ timestamp: string; nextHopId: string | null; hopCount: number | null }[]>(
    [],
  );
  communicationRows = signal<
    { timestamp: string; profile: string; success: boolean; error: string | null }[]
  >([]);
  drawerTab = 'health';
  readonly nav = [
    { path: 'overview', label: 'Overview', icon: '◫', group: 'Monitor' },
    { path: 'map', label: 'Network map', icon: '⌖', group: '' },
    { path: 'topology', label: 'Mesh topology', icon: '⌘', group: '' },
    { path: 'meters', label: 'Meter inventory', icon: '▦', group: '' },
    { path: 'profiles', label: 'DLMS profiles', icon: '≋', group: '' },
    { path: 'gateways', label: 'Gateways & sinks', icon: '▣', group: '' },
    { path: 'alerts', label: 'Network alerts', icon: '♧', group: 'Operate' },
    { path: 'incidents', label: 'Incidents', icon: '⚑', group: '' },
    { path: 'copilot', label: 'AI Copilot', icon: '✧', group: 'Intelligence' },
    { path: 'reports', label: 'Reports center', icon: '▤', group: '' },
    { path: 'simulator', label: 'Network simulator', icon: '▷', group: '' },
    { path: 'settings', label: 'Users & settings', icon: '⚙', group: '' },
  ];
  title = computed(() => this.nav.find((x) => x.path === this.view())?.label || 'Overview');
  mapQuery = signal('');
  mapSector = signal('');
  mapTopology = computed(() => {
    const all = this.topology();
    if (!all) return null;
    const meters = all.meters.filter(
      (n) =>
        (!this.mapQuery() ||
          n.meter.serial.toLowerCase().includes(this.mapQuery().toLowerCase())) &&
        (!this.mapSector() || n.meter.sectorId === this.mapSector()),
    );
    const ids = new Set(meters.map((n) => n.meter.id));
    return {
      ...all,
      meters,
      neighbors: all.neighbors.filter((n) => ids.has(n.meterId) && ids.has(n.neighborId)),
    };
  });
  async ngOnInit() {
    this.router.events.subscribe((e) => {
      if (e instanceof NavigationEnd) {
        this.view.set(e.urlAfterRedirects.split('/')[1]?.split('?')[0] || 'overview');
        void this.loadView();
      }
    });
    this.view.set(location.pathname.split('/')[1] || 'overview');
    try {
      const auth = await this.api.request<{ demoEnabled: boolean }>('/auth/config');
      if (auth.demoEnabled) await this.api.connectDemo();
      if (this.api.token()) await this.initialize();
      else this.loading.set(false);
    } catch (e) {
      this.error.set(this.message(e));
      this.loading.set(false);
    }
  }
  async openDemo() {
    try {
      await this.api.connectDemo();
      await this.initialize();
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async initialize() {
    this.error.set('');
    this.loading.set(true);
    try {
      const [s, t, c] = await Promise.all([
        this.api.get<Summary>('/network/summary'),
        this.api.get<Topology>('/network/topology'),
        this.api.get<{
          profiles: { key: string; name: string }[];
          sectors: { id: string; name: string }[];
        }>('/catalog'),
      ]);
      this.connected.set(true);
      this.summary.set(s);
      this.topology.set(t);
      this.meters.set(t.meters);
      this.catalog.set(c);
      this.to = s.dataTime.slice(0, 16);
      this.from = new Date(Date.parse(s.dataTime) - 7 * 86400000).toISOString().slice(0, 16);
      await this.loadView();
      void this.realtime();
    } catch (e) {
      this.error.set(this.message(e));
    } finally {
      this.loading.set(false);
    }
  }
  private connection: import('@microsoft/signalr').HubConnection | undefined;
  async realtime() {
    if (this.connection) return;
    try {
      const { HubConnectionBuilder } = await import('@microsoft/signalr');
      this.connection = new HubConnectionBuilder()
        .withUrl('/hubs/network', { accessTokenFactory: () => this.api.token() })
        .withAutomaticReconnect()
        .build();
      this.connection.on('networkUpdated', async (e: { dataTime: string }) => {
        if (this.summary()?.dataTime !== e.dataTime) {
          const s = await this.api.get<Summary>('/network/summary');
          this.summary.set(s);
          const t = await this.api.get<Topology>('/network/topology');
          this.topology.set(t);
          this.meters.set(t.meters);
        }
      });
      await this.connection.start();
    } catch {
      /* Refresh is available when the push connection is unavailable. */
    }
  }
  async loadView() {
    if (!this.connected()) return;
    this.error.set('');
    try {
      if (['overview', 'incidents', 'alerts'].includes(this.view()))
        this.incidents.set(await this.api.get<Incident[]>('/incidents'));
      if (this.view() === 'reports') await this.refreshReports();
      if (this.view() === 'profiles') await this.loadProfile();
      if (this.view() === 'gateways') this.gateways.set(await this.api.get('/gateways'));
      if (this.view() === 'simulator') this.simulation.set(await this.api.get('/simulation'));
      if (this.view() === 'settings') this.settings.set(await this.api.get('/settings'));
      if (this.view() === 'copilot') {
        const result = await this.api.get<{ configured: boolean }>('/copilot/status');
        this.aiConfigured.set(result.configured);
      }
      if (this.view() === 'incidents') this.workOrders.set(await this.api.get('/work-orders'));
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  message(e: unknown) {
    return e instanceof Error ? e.message : 'Unable to complete request.';
  }
  number(v: number | null | undefined, digits = 1) {
    return v == null ? '—' : v.toLocaleString('en-IN', { maximumFractionDigits: digits });
  }
  percent(v: number | null | undefined) {
    return v == null || !Number.isFinite(v) ? '—' : (v * 100).toFixed(1) + '%';
  }
  async filter() {
    try {
      const p = new URLSearchParams({ Limit: '300' });
      if (this.query) p.set('Search', this.query);
      if (this.status) p.set('Status', this.status);
      if (this.sector) p.set('SectorId', this.sector);
      if (this.rssi) p.set('RssiBelow', this.rssi);
      const result = await this.api.get<{ items: MeterView[] }>('/meters?' + p);
      this.meters.set(result.items);
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async select(m: Meter) {
    this.selected.set(m);
    this.findings.set([]);
    this.drawerTab = 'health';
    try {
      const [d, n, c, r] = await Promise.all([
        this.api.get<{ findings: Finding[] }>(`/meters/${m.id}/diagnosis`),
        this.api.get<{ neighborId: string; distanceM: number; rssi: number }[]>(
          `/meters/${m.id}/neighbors`,
        ),
        this.api.get<
          { timestamp: string; profile: string; success: boolean; error: string | null }[]
        >(`/meters/${m.id}/communication`),
        this.api.get<{ timestamp: string; nextHopId: string | null; hopCount: number | null }[]>(
          `/meters/${m.id}/routes?from=${encodeURIComponent(new Date(Date.parse(this.summary()!.dataTime) - 30 * 86400000).toISOString())}&to=${encodeURIComponent(this.summary()!.dataTime)}`,
        ),
      ]);
      this.findings.set(d.findings);
      this.neighborRows.set(n);
      this.communicationRows.set(c);
      this.routeRows.set(r);
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  nodeName(id: string | null) {
    return (
      this.topology()?.meters.find((n) => n.meter.id === id)?.meter.serial ||
      this.topology()?.sinks.find((s) => s.id === id)?.name ||
      'Disconnected'
    );
  }
  routingPath() {
    const all = this.topology();
    let m = this.selected();
    const path: string[] = [];
    const seen = new Set<string>();
    while (m && !seen.has(m.id)) {
      seen.add(m.id);
      path.push(m.serial);
      if (!m.nextHopId) {
        path.push('No route');
        break;
      }
      const sink = all?.sinks.find((s) => s.id === m!.nextHopId);
      if (sink) {
        path.push(sink.name);
        break;
      }
      m = all?.meters.find((n) => n.meter.id === m!.nextHopId)?.meter || null;
    }
    return path.join(' → ');
  }
  async scenario() {
    try {
      const result = await this.api.post<{ cause: string; meter: string }>(
        '/simulation/scenarios',
        { meterSerial: this.scenarioMeter, cause: this.scenarioCause },
      );
      await this.initialize();
      this.notice.set(
        `Controlled ${result.cause} scenario applied to ${result.meter}. Inspect its diagnostic evidence.`,
      );
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async loadProfile() {
    try {
      const id = this.selected()?.serial || this.meterSerial;
      const q = new URLSearchParams({
        from: new Date(this.from + 'Z').toISOString(),
        to: new Date(this.to + 'Z').toISOString(),
        limit: '500',
      });
      const rows = await this.api.get<{ items: Profile[] }>(
        `/meters/${encodeURIComponent(id)}/profiles/${this.profile}?${q}`,
      );
      this.profileRows.set(rows.items);
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async export(format = 'xlsx', kind = 'inventory') {
    try {
      const result = await this.api.post<{ id: string }>('/reports', {
        format,
        kind,
        filter: {
          search: this.query || null,
          rssiBelow: this.rssi ? Number(this.rssi) : null,
          status: this.status || null,
          sectorId: this.sector || null,
          limit: 1000,
        },
        ...(kind === 'profiles'
          ? {
              meterId: this.selected()?.serial || this.meterSerial,
              profile: this.profile,
              from: new Date(this.from + 'Z').toISOString(),
              to: new Date(this.to + 'Z').toISOString(),
            }
          : {}),
      });
      this.notice.set(`Report queued (${result.id.slice(0, 8)}). Download it from Reports center.`);
      if (this.view() === 'reports') await this.refreshReports();
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async refreshReports() {
    this.reports.set(await this.api.get('/reports'));
  }
  async download(r: Report) {
    try {
      await this.api.download(r.id, r.format);
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async incident(i: Incident, status: string) {
    try {
      await this.api.request(`/api/incidents/${i.id}`, {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      });
      await this.loadView();
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async simulate(action: string) {
    if (
      action === 'reset' &&
      !confirm('Reset only this isolated demo? This replaces its historical data and reports.')
    )
      return;
    try {
      this.loading.set(true);
      this.simulation.set(await this.api.post('/simulation/' + action));
      await this.initialize();
      this.notice.set('Simulation ' + action + ' completed.');
    } catch (e) {
      this.error.set(this.message(e));
    } finally {
      this.loading.set(false);
    }
  }
  async playbackAt() {
    try {
      this.topology.set(
        await this.api.get(
          '/network/topology' +
            (this.playback
              ? '?at=' + encodeURIComponent(new Date(this.playback + 'Z').toISOString())
              : ''),
        ),
      );
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async affected(id: string) {
    try {
      const result = await this.api.get<{ count: number; meters: { serial: string }[] }>(
        '/network/affected/' + id,
      );
      this.notice.set(
        `${result.count} potentially affected meters: ${result.meters
          .slice(0, 15)
          .map((x) => x.serial)
          .join(', ')}${result.count > 15 ? '…' : ''}`,
      );
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async signIn() {
    this.api.setToken(this.tokenInput.trim());
    await this.initialize();
  }
  async approve(id: string) {
    try {
      await this.api.post('/work-orders/' + id + '/approve');
      await this.loadView();
    } catch (e) {
      this.error.set(this.message(e));
    }
  }
  async send(text = this.prompt) {
    if (!text.trim() || this.sending()) return;
    this.prompt = '';
    this.sending.set(true);
    this.messages.update((x) => [
      ...x,
      { role: 'user', text, tools: [] },
      { role: 'assistant', text: '', tools: [] },
    ]);
    this.abort = new AbortController();
    try {
      const response = await fetch('/api/copilot/chat', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${this.api.token()}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ message: text, conversationId: this.conversationId }),
        signal: this.abort.signal,
      });
      if (!response.ok) throw new Error('Copilot request failed (' + response.status + ').');
      const reader = response.body!.getReader();
      const decoder = new TextDecoder();
      let pending = '';
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        pending += decoder.decode(value, { stream: true });
        const chunks = pending.split('\n\n');
        pending = chunks.pop()!;
        for (const chunk of chunks) {
          const type = chunk
            .split('\n')
            .find((x) => x.startsWith('event: '))
            ?.slice(7);
          const dataLine = chunk.split('\n').find((x) => x.startsWith('data: '));
          if (!dataLine) continue;
          const data = JSON.parse(dataLine.slice(6));
          if (type === 'error') throw new Error(data.message);
          if (type === 'conversation') this.conversationId = data.id;
          if (type === 'delta' || type === 'tool')
            this.messages.update((x) => {
              const last = x[x.length - 1];
              return [
                ...x.slice(0, -1),
                {
                  ...last,
                  text: last.text + (type === 'delta' ? data.text : ''),
                  tools: type === 'tool' ? [...last.tools, data] : last.tools,
                },
              ];
            });
        }
      }
    } catch (e) {
      this.error.set(this.message(e));
    } finally {
      this.sending.set(false);
    }
  }
  ngOnDestroy() {
    this.abort?.abort();
    void this.connection?.stop();
  }
}
