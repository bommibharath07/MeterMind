import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './e2e',
  use: { baseURL: process.env['METERMIND_UI_URL'] || 'http://127.0.0.1:4200', headless: true },
  reporter: [['list'], ['html', { open: 'never' }]],
  retries: 0,
  timeout: 60000,
});
