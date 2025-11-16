import { Page } from '@playwright/test';
import { validateExcelFile } from './excel-validator';
import * as path from 'path';
import * as fs from 'fs';
import { expect } from '@playwright/test';

/**
 * Shared helper function to run the complete week flow test
 * @param page Playwright page object
 * @param week Week number (e.g., 11, 12)
 * @param userName User name for the picks
 */
export async function testWeekFlow(page: Page, week: number, userName: string): Promise<void> {
  // Navigate to the application
  await page.goto('/');
  await page.waitForLoadState('networkidle');

  // Navigate to picks page - try to find a link to picks page
  const picksLink = page.locator('a[href="/picks"]').first();
  const picksLinkCount = await picksLink.count();
  if (picksLinkCount > 0) {
    await picksLink.click();
    await page.waitForLoadState('networkidle');
  } else {
    // Direct navigation if link not found
    await page.goto('/picks');
    await page.waitForLoadState('networkidle');
  }

  // Enter name
  const nameInput = page.locator('input#userName, input[placeholder*="name" i]');
  await nameInput.fill(userName);

  // Select year 2025
  const yearSelect = page.locator('select#year');
  await yearSelect.selectOption('2025');
  
  // Wait for weeks dropdown to be populated
  const weekSelect = page.locator('select#week');
  await weekSelect.waitFor({ state: 'attached' });
  await expect(weekSelect).not.toBeDisabled();

  // Select Week
  await weekSelect.selectOption(String(week));

  // Click Continue
  await page.getByRole('button', { name: /continue/i }).click();
  await page.waitForLoadState('networkidle');
  
  // Wait for games to load - wait for game buttons to appear
  // Game buttons have team logos, we can wait for those to ensure the page is loaded
  await page.waitForSelector('.card button', { state: 'visible', timeout: 10000 });

  // Select 6 games by clicking on team buttons
  // Game buttons are in cards and are not the "Back" button
  const gameButtons = page.locator('.card button');
  const buttonCount = await gameButtons.count();
  expect(buttonCount).toBeGreaterThanOrEqual(6);

  // Click first 6 game buttons
  for (let i = 0; i < 6 && i < buttonCount; i++) {
    const button = gameButtons.nth(i);
    await expect(button).toBeEnabled();
    await button.click();
    // Wait for the button state to update - the app uses 'btn-selected' class
    await expect(button).toHaveClass(/btn-selected/);
  }

  // Wait for download button to appear
  await page.waitForSelector('button:has-text("Download")', { timeout: 5000 });

  // Click download button and save file
  const downloadButton = page.locator('button:has-text("Download")');
  const downloadPromise = page.waitForEvent('download');
  await downloadButton.click();
  const download = await downloadPromise;

  // Save the download
  const downloadPath = path.join('/tmp', `week${week}_picks_${Date.now()}.xlsx`);
  await download.saveAs(downloadPath);

  // Verify the Excel file exists
  expect(fs.existsSync(downloadPath)).toBeTruthy();

  // Validate Excel structure and content
  await validateExcelFile(downloadPath, userName, 6);

  // Cleanup
  fs.unlinkSync(downloadPath);
}
