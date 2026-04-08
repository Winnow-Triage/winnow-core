import { test, expect } from '@playwright/test';
import fs from 'fs';
import path from 'path';

import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Output directory for screenshots
const OUTPUT_DIR = path.join(__dirname, 'output');

// Ensure output directory exists
if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

const pages = [
  { name: 'dashboard-mockup', path: '/dashboard' },
  { name: 'clusters-dashboard', path: '/clusters' },
  { name: 'triage-dashboard', path: '/dashboard' }, // Alias for marketing
  { name: 'triage-review', path: '/triage/review' },
  { name: 'triage-export', path: '/reports' },
];

const themes = ['light', 'dark'] as const;

test.describe('Marketing Screenshots', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login and use the quick demo login
    await page.goto('/login');
    const demoBtn = page.locator('#quick-demo-login');
    await demoBtn.waitFor({ state: 'visible', timeout: 10000 });
    await demoBtn.click();
    
    // Wait for dashboard to load
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 15000 });
  });

  for (const theme of themes) {
    test.describe(`${theme} theme`, () => {
      test.beforeEach(async ({ page }) => {
        // Check if the current theme matches the desired theme by inspecting the 'dark' class on <html>
        const isDarkCurrently = await page.evaluate(() => document.documentElement.classList.contains('dark'));
        const wantsDark = theme === 'dark';
        
        if (isDarkCurrently !== wantsDark) {
          // Click the toggle button to switch
          console.log(`Switching theme to ${theme}...`);
          const toggleBtn = page.getByRole('button', { name: 'Toggle theme' });
          await toggleBtn.waitFor({ state: 'visible', timeout: 5000 });
          await toggleBtn.click();
          
          // Verify theme change on the html element
          await expect(page.locator('html')).toHaveClass(wantsDark ? /dark/ : /^(?!.*dark).*$/);
        }
      });

      for (const pageInfo of pages) {
        test(`Capture ${pageInfo.name} in ${theme} mode`, async ({ page }) => {
          await page.goto(pageInfo.path);
          
          // Inject CSS to hide marketing-unfriendly elements (Banners, etc.)
          // We do this early to prevent banners from causing layout shifts or fake spinner hangs
          await page.addStyleTag({
            content: `
              /* Hide Demo Banner */
              div:has(> span:text-is("Secure Demo Sandbox")) { display: none !important; }
              /* Hide Verification Banner */
              div:has(> span:text-is("Please verify your email address")) { display: none !important; }
            `
          });

          // Wait for ALL loading spinners (Lucide Loader2 with animate-spin) to disappear
          // We use a 15s timeout to be safe, but allow the test to continue if a tiny spinner is stuck
          try {
            await page.waitForFunction(() => document.querySelectorAll('.animate-spin').length === 0, { timeout: 15000 });
          } catch {
            console.log("Warning: Spinner(s) still present after 15s. Proceeding to verify content.");
          }

          // Verify we have a heading for the page to ensure something rendered
          await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible({ timeout: 10000 });

          // Final generous wait for all data and animations to settle for marketing quality
          await page.waitForTimeout(3000);

          const screenshotPath = path.join(OUTPUT_DIR, `${pageInfo.name}-${theme}.png`);
          
          await page.screenshot({
            path: screenshotPath,
            type: 'png',
            fullPage: false, 
          });

          console.log(`Saved screenshot: ${screenshotPath}`);
        });
      }
    });
  }
});
