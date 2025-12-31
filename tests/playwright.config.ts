import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './specs',

  // Run tests sequentially
  fullyParallel: false,
  workers: 1,

  // Fail on test.only in CI
  forbidOnly: !!process.env.CI,

  // Retry on CI only
  retries: process.env.CI ? 2 : 0,

  // Reporter
  reporter: [
    ['html'],
    ['list']
  ],

  use: {
    // Use BASE_URL env var for CI/preview environments, fallback to local SWA CLI
    baseURL: process.env.BASE_URL || 'http://localhost:4280',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',

    // Increase timeouts for slower operations (longer for CI/cloud environments)
    actionTimeout: process.env.CI ? 45000 : 15000,
    navigationTimeout: process.env.CI ? 90000 : 30000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // NO global setup - services must be started manually via:
  //   ./start-local.sh && swa start http://localhost:5158 --api-devserver-url http://localhost:7071
});
