import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end tests always use a real API. Set E2E_BASE_URL to point at a deployed instance; otherwise
 * the local Vite server starts and proxies /api to VITE_DEV_API_TARGET (the API must already be running).
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  reporter: process.env['CI'] ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:5173',
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true,
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // A pinned system Chromium can be used instead of the Playwright download (CI images, sandboxes).
        ...(process.env['PW_CHROMIUM_PATH']
          ? { launchOptions: { executablePath: process.env['PW_CHROMIUM_PATH'] } }
          : {}),
      },
    },
  ],
  webServer: process.env['E2E_BASE_URL']
    ? undefined
    : {
        command: 'npm run dev',
        url: 'http://localhost:5173',
        reuseExistingServer: !process.env['CI'],
        timeout: 60_000,
      },
});
