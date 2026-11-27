import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart } from './chart';
import { MeterView, Finding, Summary } from './types';
import {ReportDownload} from './report-download';
@Component({
  selector: 'mm-evidence',
  standalone: true,
  imports: [CommonModule, Chart,ReportDownload],
  template: `
    <section class="evidence">
      <header>
        <b>Database evidence</b><code>{{ name }}</code>
      </header>
      @if (meters().length) {
        <div class="scroll">
          <table>
            <thead>
              <tr>
                <th>Meter</th>
                <th>Status</th>
                <th>RSSI dBm</th>
                <th>Read success</th>
              </tr>
            </thead>
            <tbody>
              @for (n of meters(); track n.meter.id) {
                <tr>
                  <td>{{ n.meter.serial }}</td>
                  <td>{{ n.status }}</td>
                  <td>{{ n.meter.rssi ?? 'Missing' }}</td>
                  <td>
                    {{
                      n.meter.readSuccess === null
                        ? 'Missing'
                        : ((n.meter.readSuccess || 0) * 100 | number: '1.1-1') + '%'
                    }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p>{{ count() }} matching records. Card shows a bounded result.</p>
      }
      @if (summary(); as s) {
        <p>
          <b>{{ s.total }}</b> meters · {{ s.weakSignal }} weak signal · Data clock
          {{ s.dataTime | date: 'medium' : 'UTC' }} UTC
        </p>
        <mm-chart [data]="s.trend" />
      }
      @for (f of findings(); track f.rule) {
        <article>
          <strong>{{ f.conclusion }}</strong
          ><span>{{ f.evidenceQuality }}</span>
          <p>{{ f.evidence.join('; ') }}</p>
          <p>Actions: {{ f.actions.join('; ') }}</p>
        </article>
      }
      @if (reportId()) {
        <mm-report-download [reportId]="reportId()!" [format]="name==='generate_csv_report'?'csv':name==='generate_text_report'?'txt':'xlsx'"/>
      }
      <details>
        <summary>Inspect complete tool result</summary>
        <pre>{{ result | json }}</pre>
      </details>
    </section>
  `,
  styles: [
    `
      .evidence {
        border: 1px solid #dceae4;
        border-radius: 6px;
        margin: 12px 0;
        background: #fbfefd;
        font-size: 11px;
      }
      header {
        padding: 10px 13px;
        display: flex;
        justify-content: space-between;
        border-bottom: 1px solid #e0eee8;
        color: #346d5a;
      }
      code {
        font-size: 9px;
        color: #7a9b8f;
      }
      .scroll {
        overflow: auto;
        max-height: 250px;
      }
      table {
        border-collapse: collapse;
        width: 100%;
        text-align: left;
      }
      td,
      th {
        padding: 8px 13px;
        border-bottom: 1px solid #e7f0eb;
        font-size: 10px;
      }
      th {
        color: #78968a;
        font-weight: 500;
      }
      p,
      details,
      a {
        margin: 10px 13px;
        display: block;
      }
      article {
        margin: 10px 13px;
        padding: 10px;
        background: #eff7f2;
      }
      article p {
        margin: 6px 0;
      }
      article span {
        display: block;
        font-size: 9px;
        color: #7e968b;
      }
      pre {
        max-height: 280px;
        overflow: auto;
        white-space: pre-wrap;
        font-size: 9px;
      }
      summary {
        cursor: pointer;
        color: #008f83;
      }
    `,
  ],
})
export class Evidence {
  @Input() name = '';
  @Input() result: unknown;
  private object() {
    return this.result && typeof this.result === 'object'
      ? (this.result as Record<string, unknown>)
      : {};
  }
  meters(): MeterView[] {
    const value = this.object()['items'];
    return Array.isArray(value) && value.every((x) => x && typeof x === 'object' && 'meter' in x)
      ? (value as MeterView[])
      : [];
  }
  count() {
    return this.object()['total'] ?? this.meters().length;
  }
  summary(): Summary | null {
    const o = this.object();
    return 'total' in o && Array.isArray(o['trend']) ? (o as unknown as Summary) : null;
  }
  findings(): Finding[] {
    const value = this.object()['findings'];
    return Array.isArray(value) ? (value as Finding[]) : [];
  }
  reportId() {
    return this.object()['reportId'] as string | undefined;
  }
}
