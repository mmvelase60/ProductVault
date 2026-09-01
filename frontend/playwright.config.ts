import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: process.env['CI'] ? [['html', { open: 'never' }], ['list']] : 'list',
  use: {
    baseURL: process.env['PLAYWRIGHT_BASE_URL'] ?? 'http://127.0.0.1:4300',
    trace: 'on-first-retry'
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } }
  ],
  webServer: process.env['PLAYWRIGHT_BASE_URL'] ? undefined : {
    command: 'pnpm exec ng serve --host 127.0.0.1 --port 4300',
    url: 'http://127.0.0.1:4300/login',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000
  }
});
