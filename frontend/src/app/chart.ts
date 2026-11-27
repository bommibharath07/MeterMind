import { Component, ElementRef, Input, ViewChild, OnChanges, OnDestroy } from '@angular/core';
import type { ECharts } from 'echarts';
@Component({
  selector: 'mm-chart',
  standalone: true,
  template:
    '<div #chart role="img" aria-label="Network packet delivery and read success over the last 24 hours"></div>',
  styles: ['div{height:235px;width:100%}'],
})
export class Chart implements OnChanges, OnDestroy {
  @Input() data: { hour: number; pdr: number; readSuccess: number }[] = [];
  @ViewChild('chart', { static: true }) host!: ElementRef<HTMLDivElement>;
  private chart: ECharts | undefined;
  private resize = () => this.chart?.resize();
  ngOnChanges() {
    setTimeout(() => void this.render(), 0);
  }
  async render() {
    const echarts = await import('echarts');
    if (!this.host) return;
    this.chart ??= echarts.init(this.host.nativeElement);
    this.chart.setOption({
      color: ['#008f83', '#8ebbc2'],
      tooltip: { trigger: 'axis', valueFormatter: (v: unknown) => `${Number(v).toFixed(1)}%` },
      legend: { bottom: 0, icon: 'circle', textStyle: { fontSize: 10, color: '#6d7d89' } },
      grid: { left: 44, right: 15, top: 20, bottom: 48 },
      xAxis: {
        type: 'category',
        data: this.data.map((x) => `${x.hour}:00`),
        axisLabel: { fontSize: 10, color: '#84929c' },
        axisLine: { lineStyle: { color: '#e7edf0' } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        min: 50,
        max: 100,
        axisLabel: { formatter: '{value}%', fontSize: 10, color: '#84929c' },
        splitLine: { lineStyle: { color: '#edf1f3', type: 'dashed' } },
      },
      series: [
        {
          name: 'Packet delivery',
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: { width: 2.5 },
          areaStyle: { opacity: 0.06 },
          data: this.data.map((x) => x.pdr * 100),
        },
        {
          name: 'Meter read success',
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: { width: 2, type: 'dashed' },
          data: this.data.map((x) => x.readSuccess * 100),
        },
      ],
    });
    window.addEventListener('resize', this.resize);
  }
  ngOnDestroy() {
    window.removeEventListener('resize', this.resize);
    this.chart?.dispose();
  }
}
