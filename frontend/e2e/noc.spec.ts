import { test, expect } from '@playwright/test';
test('database dashboard, all nodes, filtering and profile navigation', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible();
  await expect(page.locator('.meter-node')).toHaveCount(300, { timeout: 60000 });
  await expect(page.getByText('Simulation environment', { exact: true }).first()).toBeVisible();
  await page.getByRole('link', { name: '▦ Meter inventory' }).click();
  await page.getByRole('spinbutton', { name: 'RSSI below' }).fill('-90');
  await page.getByRole('button', { name: 'Apply filters' }).click();
  await expect(page.locator('tbody tr')).toHaveCount(18);
  await page.getByRole('button', { name: 'MM-0001', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByText('Weak signal observed')).toBeVisible();
  await page.getByRole('button', { name: 'Close meter details' }).click();
  await page.getByRole('link', { name: '≋ DLMS profiles' }).click();
  await expect(page.locator('tbody tr')).not.toHaveCount(0);
  await expect(page.locator('.profile-tabs button')).toHaveCount(10);
  await page.getByRole('button', { name: 'Power Events', exact: true }).click();
  await expect(page.getByRole('columnheader', { name: 'Event code' })).toBeVisible();
  await page.getByRole('link', { name: '✧ AI Copilot AI' }).click();
  await expect(
    page.getByRole('heading', { name: 'AI chat is paused' }),
  ).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Ask MeterMind' })).toHaveCount(0);
  await page.getByRole('link', { name: 'Open reports', exact: true }).click();
  await expect(page.locator('.report-actions')).toBeVisible();
  expect(errors).toEqual([]);
});
test('responsive navigation remains usable', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/map');
  await expect(page.locator('.meter-node')).toHaveCount(300, { timeout: 60000 });
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
  ).toBeTruthy();
});
