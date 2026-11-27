import {test,expect} from '@playwright/test';
import {stat} from 'node:fs/promises';
// Only OpenAI's narrative/tool selection is mocked; inventory, report jobs and downloads use PostgreSQL-backed APIs.
test('offline conversation shows database records and downloads Excel directly',async({page})=>{
  await page.route('**/api/copilot/status',route=>route.fulfill({json:{configured:true,model:'mocked-ui-test'}}));
  let turn=0;let expectedCount=0;
  await page.route('**/api/copilot/chat',async route=>{
    const headers={Authorization:route.request().headers()['authorization']};turn++;
    let name:string,result:unknown,text:string;
    if(turn===1){const response=await page.request.get('/api/meters?Status=offline',{headers});expect(response.ok()).toBeTruthy();const rows=await response.json();expectedCount=rows.total;name='get_offline_meters';result=rows;text=`${rows.total} simulated offline meters were found.`;}
    else{const response=await page.request.post('/api/reports',{headers,data:{format:'xlsx',kind:'inventory',filter:{status:'offline',limit:1000}}});expect(response.ok()).toBeTruthy();const queued=await response.json();name='generate_excel_report';result={reportId:queued.id,status:'queued'};text='Your offline-meter Excel export is queued.';}
    const body=`event: tool\ndata: ${JSON.stringify({name,result,queryTime:new Date().toISOString()})}\n\nevent: delta\ndata: ${JSON.stringify({text})}\n\nevent: done\ndata: {}\n\n`;
    await route.fulfill({status:200,contentType:'text/event-stream',body});
  });
  await page.goto('/copilot');await page.getByRole('textbox',{name:'Ask MeterMind'}).fill('List meters that are offline');await page.getByRole('button',{name:'Send ↗'}).click();
  await expect(page.locator('mm-evidence tbody tr')).toHaveCount(6);expect(expectedCount).toBe(6);
  await page.getByRole('textbox',{name:'Ask MeterMind'}).fill('I want those offline meters in Excel');await page.getByRole('button',{name:'Send ↗'}).click();
  const downloadButton=page.getByRole('button',{name:'Download XLSX',exact:true});await expect(downloadButton).toBeVisible({timeout:30000});
  const pending=page.waitForEvent('download');await downloadButton.click();const download=await pending;expect(download.suggestedFilename()).toMatch(/\.xlsx$/);expect((await stat((await download.path())!)).size).toBeGreaterThan(1000);
});
