import {Component,Input,OnChanges,OnDestroy,inject,signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {Api} from './api';
import {Report} from './types';
@Component({selector:'mm-report-download',standalone:true,imports:[RouterLink],template:`
<section class="download-card"><div><b>{{format.toUpperCase()}} report</b><span>{{status()}}</span></div>
@if(error()){<p role="alert">{{error()}}</p>}
@if(status()==='complete'){<button type="button" (click)="download()" [disabled]="downloading()">{{downloading()?'Downloading…':'Download '+format.toUpperCase()}}</button>}
@else if(status()==='queued'||status()==='processing'){<p role="status">Preparing your matching database records…</p>}
<a routerLink="/reports">View in Reports center</a></section>`,styles:[`.download-card{margin:12px;padding:15px;border:1px solid #c9e4d8;border-radius:6px;background:#f0f8f4;font-size:12px}.download-card>div{display:flex;justify-content:space-between;gap:20px}span{color:#577e6d;font-size:10px}button{margin:14px 0 8px;padding:9px 16px;background:#008f83;color:white;border:0;border-radius:4px;cursor:pointer;font:inherit}button:disabled{opacity:.6}a{display:block;margin-top:10px;color:#00786d;font-size:11px}p{font-size:11px;color:#547565}`]})
export class ReportDownload implements OnChanges,OnDestroy {
  @Input({required:true}) reportId='';@Input() format='xlsx';
  private api=inject(Api);private timer:ReturnType<typeof setTimeout>|undefined;private generation=0;
  status=signal('queued');error=signal('');downloading=signal(false);
  ngOnChanges(){clearTimeout(this.timer);this.status.set('queued');this.error.set('');const current=++this.generation;void this.poll(current,0);}
  private async poll(current:number,attempt:number){try{const reports=await this.api.get<Report[]>('/reports');if(current!==this.generation)return;const report=reports.find(r=>r.id===this.reportId);if(report){this.status.set(report.status);if(report.status==='failed'){this.error.set(report.error||'Report generation failed.');return;}if(report.status==='expired'){this.error.set('This report expired. Request a new export.');return;}if(report.status==='complete')return;}if(attempt<60)this.timer=setTimeout(()=>void this.poll(current,attempt+1),1000);else this.error.set('The report is still processing. Check Reports center.');}catch(e){if(current===this.generation)this.error.set(e instanceof Error?e.message:'Unable to check the report.');}}
  async download(){this.downloading.set(true);try{await this.api.download(this.reportId,this.format);}catch(e){this.error.set(e instanceof Error?e.message:'Unable to download the report.');}finally{this.downloading.set(false);}}
  ngOnDestroy(){this.generation++;clearTimeout(this.timer);}
}
