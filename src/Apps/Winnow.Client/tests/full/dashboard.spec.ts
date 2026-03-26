import { test, expect } from '@playwright/test';

test.describe('Dashboard & Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[type="email"]', 'admin@winnowtriage.com');
    await page.fill('input[type="password"]', 'P@ssword123!');
    await page.click('button:has-text("Sign In")');
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 15000 });
  });

  test('Verify Sidebar Navigation', async ({ page }) => {
    await page.click('text=All Reports');
    await expect(page).toHaveURL(/.*reports/);

    await page.click('text=Clusters');
    await expect(page).toHaveURL(/.*clusters/);

    await page.click('text=Org Overview');
    await expect(page).toHaveURL(/.*org-dashboard/);
  });

  test('Dashboard Metrics Visibility', async ({ page }) => {
    await expect(page.locator('card-title, h3, div').getByText('Winnow Ratio')).toBeVisible();
    await expect(page.locator('card-title, h3, div').getByText('Pending Decisions')).toBeVisible();
    await expect(page.locator('card-title, h3, div').getByText('Hottest Clusters')).toBeVisible();
  });
});
