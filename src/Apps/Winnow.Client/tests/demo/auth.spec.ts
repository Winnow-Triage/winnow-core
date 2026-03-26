import { test, expect } from '@playwright/test';

test.describe('Demo Mode Authentication', () => {
  test('Demo Mode: Quick Login Shortcut', async ({ page }) => {
    await page.goto('/login');
    
    // Explicitly wait for the button as it's part of an animated entry
    const demoBtn = page.locator('#quick-demo-login');
    await demoBtn.waitFor({ state: 'visible', timeout: 10000 });
    await demoBtn.click();

    // Verify redirection to dashboard via demo mode
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 15000 });
    await expect(page.locator('text=TB')).toBeVisible(); // TB initials for "Truman Burbank"
  });

  test('Logout Flow (Demo)', async ({ page }) => {
    await page.goto('/login');
    await page.locator('#quick-demo-login').waitFor({ state: 'visible' });
    await page.click('#quick-demo-login');

    await expect(page).toHaveURL(/.*dashboard/);

    // Click Avatar to open menu
    await page.click('button:has-text("TB")'); 
    await page.click('text=Log out');

    await expect(page).toHaveURL(/.*login/);
  });
});
