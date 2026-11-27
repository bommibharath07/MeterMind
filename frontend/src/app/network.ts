import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  OnChanges,
  OnDestroy,
  inject,
  ChangeDetectorRef,
  SimpleChanges,
} from '@angular/core';
import { Meter, Topology } from './types';
@Component({
  selector: 'mm-network',
  standalone: true,
  template: `
    <div class="network-toolbar">
      <span
        ><i class="dot online"></i> Online <i class="dot degraded"></i> Degraded
        <i class="dot offline"></i> Offline</span
      >
      <div>
        <button [disabled]="mode!=='local'" title="Potential neighbor overlay is available in local coordinates" (click)="showNeighbors = !showNeighbors">
          {{ showNeighbors ? 'Hide' : 'Show' }} neighbors</button
        ><button (click)="geographic()">Geographic map</button
        ><button (click)="local()">Local coordinates</button>
      </div>
    </div>
    <div #host class="network-host" [hidden]="mode === 'local'"></div>
    @if (mode === 'local') {
      <svg
        class="mesh-svg"
        viewBox="-1120 -1120 2240 2240"
        role="img"
        aria-label="RF mesh network. Select a meter to inspect it."
      >
        <defs>
          <pattern id="grid" width="150" height="150" patternUnits="userSpaceOnUse">
            <path d="M 150 0 L 0 0 0 150" fill="none" stroke="#dfe7e9" stroke-width="2" />
          </pattern>
        </defs>
        <rect x="-1120" y="-1120" width="2240" height="2240" fill="url(#grid)" />
        <circle r="1000" class="boundary" />
        <path
          d="M-1050 230 Q-300 420 0 150 T1050 -270"
          fill="none"
          stroke="#d3e9ee"
          stroke-width="90"
        />
        <path d="M-850 -850 L900 800 M-900 650 L800 -800" stroke="#fff" stroke-width="28" />
        @for (edge of edges(); track edge.id) {
          <line
            [attr.x1]="edge.x1"
            [attr.y1]="edge.y1"
            [attr.x2]="edge.x2"
            [attr.y2]="edge.y2"
            [class.potential]="edge.potential"
            class="route-link"
          />
        }
        @for (node of data?.meters || []; track node.meter.id) {
          <circle
            class="meter-node"
            [class]="'meter-node ' + node.status"
            [attr.cx]="node.meter.x"
            [attr.cy]="-node.meter.y"
            r="10"
            tabindex="0"
            role="button"
            [attr.aria-label]="node.meter.serial + ' ' + node.status"
            (click)="selected.emit(node.meter)"
            (keydown.enter)="selected.emit(node.meter)"
          >
            <title>{{ node.meter.serial }} · {{ node.meter.rssi }} dBm · {{ node.status }}</title>
          </circle>
        }
        @for (sink of data?.sinks || []; track sink.id) {
          <g [attr.transform]="'translate(' + sink.x + ',' + -sink.y + ')'">
            <rect
              x="-24"
              y="-24"
              width="48"
              height="48"
              rx="9"
              fill="#142e3d"
              stroke="white"
              stroke-width="8"
            />
            <text y="-42" text-anchor="middle" class="sink-label">{{ sink.name }}</text>
          </g>
        }
        <text x="-1050" y="1040" class="map-caption">
          Local RF survey · 1 km deployment radius · routing links shown
        </text>
      </svg>
    }
    @if (note) {
      <p class="map-note">{{ note }}</p>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .network-toolbar {
        display: flex;
        justify-content: space-between;
        gap: 10px;
        align-items: center;
        padding: 14px 20px;
        border-bottom: 1px solid #e7ecef;
        font-size: 11px;
      }
      .network-toolbar span {
        display: flex;
        gap: 6px;
        align-items: center;
      }
      .network-toolbar button {
        background: white;
        border: 1px solid #dce4e7;
        padding: 6px 9px;
        margin-left: 5px;
        border-radius: 4px;
        color: #46606e;
        font-size: 11px;
        cursor: pointer;
      }
      .mesh-svg,
      .network-host {
        width: 100%;
        height: 490px;
        display: block;
        background: #f2f6f5;
      }
      .network-host[hidden] {
        display: none;
      }
      .boundary {
        fill: none;
        stroke: #c9d8d8;
        stroke-dasharray: 9 12;
        stroke-width: 3;
      }
      .route-link {
        stroke: #008f83;
        stroke-width: 2;
        opacity: 0.25;
      }
      .potential {
        stroke: #6b7d8a;
        opacity: 0.1;
        stroke-dasharray: 4 6;
      }
      .meter-node {
        stroke: white;
        stroke-width: 3;
        cursor: pointer;
      }
      .meter-node:hover,
      .meter-node:focus {
        stroke: #142e3d;
        stroke-width: 8;
        outline: none;
      }
      .online {
        fill: #16a34a;
      }
      .degraded {
        fill: #e9a23b;
      }
      .offline,
      .unknown {
        fill: #64748b;
      }
      .sink-label {
        font:
          23px 'Segoe UI',
          sans-serif;
        fill: #142e3d;
        font-weight: 600;
      }
      .map-caption {
        font:
          21px 'Segoe UI',
          sans-serif;
        fill: #6d838a;
      }
      .map-note {
        font-size: 11px;
        margin: 10px 20px;
        color: #64748b;
      }
      @media (max-width: 700px) {
        .network-toolbar {
          flex-wrap: wrap;
        }
        .mesh-svg,
        .network-host {
          height: 340px;
        }
      }
    `,
  ],
})
export class Network implements OnChanges, OnDestroy {
  private cdr=inject(ChangeDetectorRef);
  private logicalInitialized=false;
  @Input() data: Topology | null = null;
  @Input() logical = false;
  @Output() selected = new EventEmitter<Meter>();
  @ViewChild('host', { static: true }) host!: ElementRef<HTMLDivElement>;
  mode = 'local';
  showNeighbors = false;
  note = '';
  private cleanup: (() => void) | null = null;
  ngOnChanges(changes:SimpleChanges) {
    if(!this.logical&&changes['logical'])this.local();
    if (this.logical && this.data&&(!this.logicalInitialized||changes['logical']||this.mode==='logical')){this.logicalInitialized=true;setTimeout(() => void this.cytoscape(), 0);}
    else if(this.data&&this.mode==='geographic'&&changes['data'])setTimeout(()=>void this.geographic(),0);
  }
  edges() {
    const all = this.data;
    if (!all) return [];
    const position = new Map<string, { x: number; y: number }>();
    all.meters.forEach((n) => position.set(n.meter.id, n.meter));
    all.sinks.forEach((n) => position.set(n.id, n));
    const list = all.meters
      .filter((n) => n.meter.nextHopId)
      .map((n) => ({ id: n.meter.id, from: n.meter.id, to: n.meter.nextHopId!, potential: false }));
    if (this.showNeighbors)
      all.neighbors.forEach((n, i) =>
        list.push({ id: 'p' + i, from: n.meterId, to: n.neighborId, potential: true }),
      );
    return list.flatMap((e) => {
      const a = position.get(e.from),
        b = position.get(e.to);
      return a && b
        ? [{ id: e.id, x1: a.x, y1: -a.y, x2: b.x, y2: -b.y, potential: e.potential }]
        : [];
    });
  }
  local() {
    this.cleanup?.();
    this.cleanup = null;
    this.mode = 'local';
    this.note = '';
    this.cdr.markForCheck();
  }
  async cytoscape() {
    try {
      this.local();
      this.mode = 'logical';
      this.cdr.detectChanges();
      const { default: cytoscape } = await import('cytoscape');
      const all = this.data!;
      const cy = cytoscape({
        container: this.host.nativeElement,
        elements: [
          ...all.meters.map((n) => ({
            data: {
              id: n.meter.id,
              label: n.meter.serial,
              color:
                n.status === 'online' ? '#16a34a' : n.status === 'degraded' ? '#e9a23b' : '#64748b',
            },
            position: { x: n.meter.x, y: -n.meter.y },
          })),
          ...all.sinks.map((s) => ({
            data: { id: s.id, label: s.name, color: '#142e3d' },
            position: { x: s.x, y: -s.y },
          })),
          ...all.meters
            .filter(
              (n) =>
                n.meter.nextHopId &&
                (all.meters.some((p) => p.meter.id === n.meter.nextHopId) ||
                  all.sinks.some((p) => p.id === n.meter.nextHopId)),
            )
            .map((n) => ({
              data: { id: 'e' + n.meter.id, source: n.meter.id, target: n.meter.nextHopId! },
            })),
        ],
        layout: { name: 'preset', padding: 25 },
        style: [
          {
            selector: 'node',
            style: {
              'background-color': 'data(color)',
              width: 15,
              height: 15,
              label: 'data(label)',
              'font-size': 7,
              color: '#46606e',
              'text-margin-y': -9,
            },
          },
          {
            selector: 'edge',
            style: {
              width: 1,
              'line-color': '#9dc9c1',
              'target-arrow-color': '#008f83',
              'target-arrow-shape': 'triangle',
              'curve-style': 'straight',
            },
          },
        ],
      });
      cy.on('tap', 'node', (e) => {
        const n = all.meters.find((n) => n.meter.id === e.target.id());
        if (n) this.selected.emit(n.meter);
      });
      this.cleanup = () => cy.destroy();
    } catch {
      this.local();
      this.note = 'Logical renderer unavailable. Showing local routing coordinates.';
    }
  }
  async geographic() {
    try {
      this.local();
      this.mode = 'geographic';
      this.cdr.detectChanges();
      const ml = await import('maplibre-gl');
      const all = this.data!;
      const center = all.gateways[0];
      const map = new ml.Map({
        container: this.host.nativeElement,
        center: [center.longitude, center.latitude],
        zoom: 14,
        style: {
          version: 8,
          sources: {
            osm: {
              type: 'raster',
              tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
              tileSize: 256,
              attribution: '© OpenStreetMap contributors',
            },
          },
          layers: [{ id: 'base', type: 'raster', source: 'osm' }],
        },
      });
      this.cleanup = () => map.remove();
      map.addControl(new ml.NavigationControl());
      map.on('error', () => {
        this.note = 'External map tiles may be unavailable. Local coordinates remain available.';
        this.cdr.markForCheck();
      });
      map.on('load', () => {
        map.addSource('meters', {
          type: 'geojson',
          cluster: true,
          clusterRadius: 25,
          data: {
            type: 'FeatureCollection',
            features: all.meters.map((n) => ({
              type: 'Feature',
              geometry: { type: 'Point', coordinates: [n.meter.longitude, n.meter.latitude] },
              properties: {
                id: n.meter.id,
                color:
                  n.status === 'online'
                    ? '#16a34a'
                    : n.status === 'degraded'
                      ? '#e9a23b'
                      : '#64748b',
              },
            })),
          },
        });
        map.addLayer({
          id: 'clusters',
          type: 'circle',
          source: 'meters',
          filter: ['has', 'point_count'],
          paint: { 'circle-radius': 18, 'circle-color': '#008f83', 'circle-opacity': 0.7 },
        });
        map.addLayer({
          id: 'meters',
          type: 'circle',
          source: 'meters',
          filter: ['!', ['has', 'point_count']],
          paint: {
            'circle-radius': 5,
            'circle-color': ['get', 'color'],
            'circle-stroke-color': 'white',
            'circle-stroke-width': 1,
          },
        });
        const coords = new Map(
          all.meters.map((n) => [n.meter.id, [n.meter.longitude, n.meter.latitude]]),
        );
        all.sinks.forEach((s) => {
          const g = all.gateways.find((g) => g.id === s.gatewayId);
          if (g) {
            coords.set(s.id, [g.longitude, g.latitude]);
            new ml.Marker({ color: '#142e3d' })
              .setLngLat([g.longitude, g.latitude])
              .setPopup(new ml.Popup().setText(g.name + ' / ' + s.name))
              .addTo(map);
          }
        });
        map.addSource('routes', {
          type: 'geojson',
          data: {
            type: 'FeatureCollection',
            features: all.meters
              .filter((n) => n.meter.nextHopId && coords.has(n.meter.nextHopId))
              .map((n) => ({
                type: 'Feature',
                properties: {},
                geometry: {
                  type: 'LineString',
                  coordinates: [coords.get(n.meter.id)!, coords.get(n.meter.nextHopId!)!],
                },
              })),
          },
        });
        map.addLayer(
          {
            id: 'routes',
            type: 'line',
            source: 'routes',
            paint: { 'line-color': '#008f83', 'line-width': 1, 'line-opacity': 0.45 },
          },
          'meters',
        );
        map.on('click', 'meters', (e) => {
          const n = all.meters.find((n) => n.meter.id === e.features?.[0]?.properties?.['id']);
          if (n) this.selected.emit(n.meter);
        });
      });
    } catch {
      this.local();
      this.note = 'Geographic rendering unavailable. All meters are shown in local coordinates.';
    }
  }
  ngOnDestroy() {
    this.cleanup?.();
  }
}
