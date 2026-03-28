/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test';

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI or locally for service stability. */
  workers: 1,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    // baseURL: 'http://localhost:5173',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'full-stack',
      testDir: './tests/full',
      use: { 
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5173',
      },
    },
    {
      name: 'demo-mode',
      testDir: './tests/demo',
      use: { 
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5174',
      },
    },
    {
      name: 'screenshots',
      testDir: './tests/screenshots',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5174',
        viewport: { width: 1920, height: 1080 },
        deviceScaleFactor: 2,
      },
    },
  ],

  /* Run your local dev server before starting the tests */
  webServer: [
    {
      // Full Stack: Native port 5173
      command: 'VITE_DEMO_MODE=false vite --port 5173 --strictPort',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      // Demo Mode: Port 5174
      command: 'VITE_DEMO_MODE=true vite --port 5174 --strictPort',
      url: 'http://localhost:5174',
      reuseExistingServer: !process.env.CI,
      stdout: 'ignore',
      stderr: 'pipe',
    }
  ],
});
