import {test,expect} from '@playwright/test';
import {stat} from 'node:fs/promises';
test('asynchronous Excel export downloads a genuine workbook',async({page})=>{
  await page.goto('/reports');await expect(page.getByRole('heading',{name:'Reports center',exact:true}).first()).toBeVisible();
  await page.locator('.report-actions > div').filter({hasText:/weak-signal/i}).getByRole('button',{name:'Excel',exact:true}).click();
  await expect(page.getByRole('status').filter({hasText:'Report queued'})).toBeVisible();
  const row=page.locator('tbody tr').filter({hasText:/weak-signal/i}).first();
  await expect.poll(async()=>{await page.getByRole('button',{name:'Refresh jobs'}).click();return row.locator('.badge').textContent();},{timeout:30000,intervals:[1000]}).toBe('complete');
  const pending=page.waitForEvent('download');await row.getByRole('button',{name:'Download XLSX'}).click();const download=await pending;expect(download.suggestedFilename()).toMatch(/\.xlsx$/);expect((await stat((await download.path())!)).size).toBeGreaterThan(1000);
});
test('logical graph renders and map search narrows actual nodes',async({page})=>{
  const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));await page.goto('/topology');await expect(page.locator('.network-host')).toBeVisible();await expect(page.locator('.network-host canvas').first()).toBeVisible();
  await page.getByRole('button',{name:'Local coordinates',exact:true}).click();await expect(page.locator('.meter-node')).toHaveCount(300);
  await page.getByRole('textbox',{name:'Search network map'}).fill('MM-0012');await expect(page.locator('.meter-node')).toHaveCount(1);expect(errors).toEqual([]);
});
