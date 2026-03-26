import { test, expect } from '@playwright/test';

test.describe('Project Management', () => {
  test.beforeEach(async ({ page }) => {
    // Login with admin for project management
    await page.goto('/login');
    await page.fill('input[type="email"]', 'admin@winnowtriage.com');
    await page.fill('input[type="password"]', 'P@ssword123!');
    await page.click('button:has-text("Sign In")');
    await expect(page).toHaveURL(/.*dashboard/);
  });

  test('Create and Switch Projects', async ({ page }) => {
    // Helper to open project dropdown and wait for it
    const openProjectDropdown = async () => {
      await page.getByTestId('project-switcher').click();
      await page.locator('[role="menu"]').waitFor({ state: 'visible' });
    };

    // 1. Open project switcher
    await openProjectDropdown();
    
    // 2. Click "Add project" inside the dropdown
    await page.getByRole('menuitem', { name: 'Add project' }).click();
    
    const projectName = `Auto Project ${Math.floor(Math.random() * 1000)}`;
    await page.fill('#projectName', projectName);
    await page.click('button:has-text("Create Project")');

    // 3. Should redirect to /setup for the new project
    await expect(page).toHaveURL(/.*setup/);
    
    // 4. Switch back to Default Project
    await page.waitForLoadState('networkidle');
    await openProjectDropdown();
    await page.getByRole('menuitem', { name: 'Default Project' }).click();
    
    // Stabilize after project switch
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    // 5. Switch to the new project to delete it
    await openProjectDropdown();
    await page.getByRole('menuitem', { name: projectName }).click();
    await page.waitForLoadState('networkidle');

    // 6. Cleanup: Delete the newly created project
    await page.goto('/project-settings');
    await page.waitForLoadState('networkidle');
    await page.click('button:has-text("Delete Project")');
    await page.click('button:has-text("Yes, delete everything")');
    
    // Should redirect after deletion
    await expect(page).toHaveURL(/.*dashboard|.*org-dashboard/);
  });
});
