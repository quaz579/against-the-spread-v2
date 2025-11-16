import { test, expect } from '@playwright/test';
import { validateExcelFile } from '../helpers/excel-validator';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Smoke test for Week 12 complete user flow
 */
test.describe('Week 12 Smoke Tests', () => {
  test('should complete full user flow for Week 12', async ({ page }) => {
    // Navigate to the application
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Navigate to picks page
    const picksLink = page.locator('text=/make.*picks/i, a[href="/picks"]').first();
    if (await picksLink.count() > 0) {
      await picksLink.click();
    } else {
      await page.goto('/picks');
    }
    await page.waitForLoadState('networkidle');

    // Enter name
    const nameInput = page.locator('input#userName, input[placeholder*="name" i]');
    await nameInput.fill('Test User Week 12');

    // Select year 2025
    const yearSelect = page.locator('select#year');
    await yearSelect.selectOption('2025');
    await page.waitForTimeout(1000); // Wait for weeks to load

    // Select Week 12
    const weekSelect = page.locator('select#week');
    await weekSelect.selectOption('12');

    // Click Continue
    await page.getByRole('button', { name: /continue/i }).click();
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000); // Wait for games to load

    // Select 6 games by clicking on team buttons
    const gameButtons = page.locator('button.btn:not(:has-text("Back"))');
    const buttonCount = await gameButtons.count();
    expect(buttonCount).toBeGreaterThanOrEqual(6);

    // Click first 6 game buttons
    for (let i = 0; i < 6 && i < buttonCount; i++) {
      const button = gameButtons.nth(i);
      if (await button.isEnabled()) {
        await button.click();
        await page.waitForTimeout(500);
      }
    }

    // Wait for download button to appear
    await page.waitForSelector('button:has-text("Download")', { timeout: 5000 });

    // Click download button and save file
    const downloadButton = page.locator('button:has-text("Download")');
    const downloadPromise = page.waitForEvent('download');
    await downloadButton.click();
    const download = await downloadPromise;

    // Save the download
    const downloadPath = path.join('/tmp', `week12_picks_${Date.now()}.xlsx`);
    await download.saveAs(downloadPath);

    // Verify the Excel file exists
    expect(fs.existsSync(downloadPath)).toBeTruthy();

    // Validate Excel structure and content
    await validateExcelFile(downloadPath, 'Test User Week 12', 6);

    // Cleanup
    fs.unlinkSync(downloadPath);
  });
});
