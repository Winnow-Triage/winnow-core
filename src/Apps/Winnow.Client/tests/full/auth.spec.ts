import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test('Full Stack Login: Real Credentials', async ({ page }) => {
    await page.goto('/');
    
    // Should redirect to login if not authenticated
    await expect(page).toHaveURL(/.*login/);

    await page.fill('input[type="email"]', 'admin@winnowtriage.com');
    await page.fill('input[type="password"]', 'P@ssword123!');
    await page.click('button:has-text("Sign In")');

    // Wait for navigation to dashboard - using redirect to /dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('button:has-text("SA")')).toBeVisible(); // SA for System Admin initials
  });

  test('Logout Flow', async ({ page }) => {
    await page.goto('/login');
    
    // Perform login
    await page.fill('input[type="email"]', 'admin@winnowtriage.com');
    await page.fill('input[type="password"]', 'P@ssword123!');
    await page.click('button:has-text("Sign In")');

    await expect(page).toHaveURL(/.*dashboard/);

    // Click Avatar to open menu
    await page.click('button:has-text("SA")'); 
    await page.click('text=Log out');

    await expect(page).toHaveURL(/.*login/);
  });
});
