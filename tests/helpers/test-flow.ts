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

  // Check if there's an error message and retry multiple times if needed
  // The page might show an error initially if data isn't ready yet
  let retryCount = 0;
  const maxRetries = 5;
  while (retryCount < maxRetries) {
    const errorAlert = page.locator('.alert-danger');
    const errorCount = await errorAlert.count();
    if (errorCount > 0) {
      console.log(`Error detected on page (attempt ${retryCount + 1}/${maxRetries}), clicking Retry button...`);
      const retryButton = page.locator('button:has-text("Retry")');
      await retryButton.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000); // Give the API call more time to complete
      retryCount++;
    } else {
      break;
    }
  }

  // Wait for the form to be visible
  // The page should now show the form with year selector
  await page.waitForSelector('select#year', { state: 'visible', timeout: 15000 });

  // Enter name
  const nameInput = page.locator('input#userName, input[placeholder*="name" i]');
  await nameInput.fill(userName);

  // Don't change the year - use the current year (2025) which is already selected
  // Just wait for weeks dropdown to be populated
  const weekSelect = page.locator('select#week');
  await weekSelect.waitFor({ state: 'attached' });
  await expect(weekSelect).not.toBeDisabled();

  // Select Week
  await weekSelect.selectOption(String(week));
  
  // Give Blazor extra time to bind the selected week value
  await page.waitForTimeout(3000);
  
  // Try to click the Continue button - it should work now after the longer wait
  const continueButton = page.locator('button.btn-primary.btn-lg');
  
  // Use Playwright's click with force option and wait for navigation
  await Promise.all([
    page.waitForLoadState('networkidle'),
    continueButton.click({ force: true })
  ]);
  
  // Wait for loading spinner to disappear (indicates games are loading)
  await page.waitForSelector('.spinner-border', { state: 'hidden', timeout: 10000 });
  
  // Wait for games to load - wait for game buttons to appear inside cards
  // The page structure has buttons inside .card .card-body elements
  await page.waitForSelector('.card .card-body button', { state: 'visible', timeout: 15000 });

  // Select 6 games by clicking on team buttons
  // Game buttons are in .card .card-body and there are 2 buttons per game (favorite and underdog)
  const gameButtons = page.locator('.card .card-body button');
  const buttonCount = await gameButtons.count();
  console.log(`Found ${buttonCount} game buttons`);
  expect(buttonCount).toBeGreaterThanOrEqual(12); // At least 6 games * 2 buttons each

  // Click first 6 game buttons (every other button since there are 2 per game)
  // We want to select 6 different games, so we click buttons at index 0, 2, 4, 6, 8, 10
  for (let i = 0; i < 12 && i < buttonCount; i += 2) {
    const button = gameButtons.nth(i);
    
    // Wait for button to be enabled (initially some might be disabled if 6 already selected)
    await expect(button).toBeEnabled({ timeout: 2000 });
    await button.click();
    
    // Wait for the button state to update - the app uses 'btn-selected' class
    await expect(button).toHaveClass(/btn-selected/, { timeout: 2000 });
    
    // Small delay to let the UI update
    await page.waitForTimeout(200);
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
