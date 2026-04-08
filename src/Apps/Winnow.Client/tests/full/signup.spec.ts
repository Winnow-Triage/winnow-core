import { test, expect } from '@playwright/test';

test.describe('Sign Up & Onboarding', () => {
  test('Complete Registration Flow (Full Stack)', async ({ page }) => {
    const uniqueId = Math.floor(Math.random() * 1000000);
    const email = `testuser${uniqueId}@example.com`;
    const fullName = `Test User ${uniqueId}`;
    const password = 'P@ssword123!';

    await page.goto('/signup');
    await expect(page.locator('h1')).toContainText('Create your account');

    await page.fill('#name', fullName);
    await page.fill('#email', email);
    await page.fill('#password', password);
    await page.fill('#confirmPassword', password);

    // Agree to Terms
    await page.click('#terms');

    // Click "Get Started"
    await page.click('button:has-text("Get Started")');

    // Should redirect to /setup
    await expect(page).toHaveURL(/.*setup/);
    await expect(page.locator('h1')).toContainText('Initialize Winnow');

    // Verify API Key is present
    const apiKey = page.locator('.font-mono .text-primary');
    await expect(apiKey).not.toBeEmpty();
    
    // Verify initials in UserNav
    const initials = fullName.split(' ').map(n => n[0]).join('').substring(0, 2);
    await expect(page.locator(`button:has-text("${initials}")`)).toBeVisible();
  });
});
