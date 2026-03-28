import { test, expect, request } from '@playwright/test';

test.describe('Triage Flow & Data Ingestion', () => {
  test('Ingest Report via API and Verify in UI', async ({ page }) => {
    test.setTimeout(60_000); // Allow extra time for async pipeline processing
    // 1. Login to get session
    await page.goto('/login');
    await page.fill('input[type="email"]', 'admin@winnowtriage.com');
    await page.fill('input[type="password"]', 'P@ssword123!');
    await page.click('button:has-text("Sign In")');
    await expect(page).toHaveURL(/.*dashboard/);

    // 2. Create a fresh project to get a clean API Key
    await page.getByTestId('project-switcher').click();
    await page.click('text=Add project');
    const triageProj = `Ingestion Test ${Date.now()}`;
    await page.fill('#projectName', triageProj);
    await page.click('button:has-text("Create Project")');
    
    // 3. Extract API Key from setup page
    await expect(page).toHaveURL(/.*setup/);
    const apiKeyEl = page.locator('.font-mono .text-primary').first();
    await expect(apiKeyEl).toBeVisible();
    const apiKey = await apiKeyEl.textContent();
    expect(apiKey).toBeTruthy();

    // 4. Ingest via API.
    //    The .NET API runs on port 5000 (docker-compose: ASPNETCORE_HTTP_PORTS=5000, host networking).
    //    When running locally via `dotnet run`, it uses port 5294 (launchSettings.json).
    //    We try port 5000 first (containerized), then fallback to 5294 (local dev).
    const apiContext = await request.newContext();
    const reportTitle = `E2E Error: ${Date.now()}`;
    const payload = {
      Title: reportTitle,
      Message: "This is a test report from Playwright E2E.",
      Metadata: { browser: "Chromium", test: "triage-flow" }
    };
    const headers = {
      'X-Winnow-Key': apiKey!.trim(),
      'Content-Type': 'application/json'
    };

    let response = await apiContext.post('http://localhost:5000/reports', {
      headers,
      data: payload
    }).catch(() => null);

    // Fallback to local dev port if containerized port is unavailable
    if (!response || !response.ok()) {
      response = await apiContext.post('http://localhost:5294/reports', {
        headers,
        data: payload
      });
    }
    
    expect(response.ok()).toBeTruthy();

    // 5. Switch to the new project so reports are scoped correctly
    await page.getByTestId('project-switcher').click();
    await page.locator('[role="menu"]').waitFor({ state: 'visible' });
    await page.getByRole('menuitem', { name: triageProj }).click();
    await page.waitForLoadState('networkidle');

    // 6. Verify in UI — navigate to All Reports via sidebar link
    await page.waitForTimeout(3000);
    await page.getByRole('link', { name: 'All Reports' }).click();
    await page.waitForLoadState('networkidle');
    await expect(page.getByText('E2E Error').first()).toBeVisible({ timeout: 15000 });
  });
});
