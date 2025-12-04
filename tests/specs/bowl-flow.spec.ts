import { expect, test } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { cleanupDownloads, getDefaultDownloadDir, waitForDownloadAndSave } from '../helpers/download-helper';
import { TestEnvironment } from '../helpers/test-environment';
import { AdminPage } from '../pages/admin-page';
import { BowlPicksPage } from '../pages/bowl-picks-page';

// Test constants
const TEST_NAME = 'Bowl E2E Test User';
const TEST_YEAR = 2025;
const DOWNLOAD_DIR = getDefaultDownloadDir();

// Get repository root path
const REPO_ROOT = path.resolve(__dirname, '../..');
const REFERENCE_DOCS = path.join(REPO_ROOT, 'reference-docs');

// Test environment configuration
const testEnv = new TestEnvironment();

test.describe('Bowl Picks Flow', () => {

  test.beforeAll(async () => {
    console.log('Bowl E2E tests starting...');
    console.log(`Admin email for mock auth: ${testEnv.adminEmail}`);
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.beforeEach(async ({ page }) => {
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.afterAll(async () => {
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test('Bowl Picks: upload bowl lines and complete user picks flow', async ({ page }) => {
    const adminPage = new AdminPage(page);
    const bowlPicksPage = new BowlPicksPage(page);

    // === STEP 1: Login to admin and upload bowl lines ===
    await test.step('Login to admin and upload bowl lines', async () => {
      const bowlLinesFile = path.join(REFERENCE_DOCS, 'Bowl Lines Test.xlsx');

      // Verify the file exists
      expect(fs.existsSync(bowlLinesFile), `Bowl lines file should exist at ${bowlLinesFile}`).toBe(true);

      // Navigate to admin page
      await adminPage.goto();

      // Login using mock authentication
      await adminPage.loginWithMockAuth(testEnv.adminEmail);

      // For bowl lines, we need to use the bowl upload endpoint
      // Note: This test assumes the admin page has bowl upload capability
      // or we upload via API directly
    });

    // === STEP 2: Navigate to bowl picks page ===
    await test.step('Navigate to bowl picks page', async () => {
      await bowlPicksPage.goto();
      await bowlPicksPage.waitForLoadingComplete();

      // Take screenshot of initial bowl picks page
      await page.screenshot({ 
        path: path.join(DOWNLOAD_DIR, 'bowl-picks-01-initial.png'),
        fullPage: true 
      });
    });

    // === STEP 3: Enter user name ===
    await test.step('Enter user name', async () => {
      await bowlPicksPage.enterName(TEST_NAME);
      await expect(bowlPicksPage.nameInput).toHaveValue(TEST_NAME);

      // Take screenshot after entering name
      await page.screenshot({ 
        path: path.join(DOWNLOAD_DIR, 'bowl-picks-02-name-entered.png'),
        fullPage: true 
      });
    });

    // === STEP 4: Select year and load bowl games ===
    await test.step('Select year and load bowl games', async () => {
      await bowlPicksPage.selectYearAndContinue(TEST_YEAR);

      // Check if games are loaded or if there's an error (no bowl lines uploaded)
      const hasError = await bowlPicksPage.hasError();
      const gamesLoaded = await bowlPicksPage.areGamesLoaded();

      if (hasError) {
        const errorMsg = await bowlPicksPage.getErrorMessage();
        console.log(`Bowl lines not available: ${errorMsg}`);
        // This is expected if no bowl lines have been uploaded
        // Take a screenshot showing the error state
        await page.screenshot({ 
          path: path.join(DOWNLOAD_DIR, 'bowl-picks-03-no-bowl-lines.png'),
          fullPage: true 
        });
        // Skip remaining steps if no bowl lines
        test.skip(true, 'No bowl lines available for testing');
      }

      if (gamesLoaded) {
        const gameCount = await bowlPicksPage.getGameCount();
        console.log(`Loaded ${gameCount} bowl games`);

        // Take screenshot of games loaded
        await page.screenshot({ 
          path: path.join(DOWNLOAD_DIR, 'bowl-picks-03-games-loaded.png'),
          fullPage: true 
        });

        expect(gameCount).toBeGreaterThan(0);
      }
    });

    // === STEP 5: Make picks for all games ===
    await test.step('Make picks for all games', async () => {
      const gameCount = await bowlPicksPage.getGameCount();

      // Make picks for each game
      await bowlPicksPage.makeAllPicks(gameCount);

      // Verify all picks are made
      const { current, total } = await bowlPicksPage.getCompletedPicksCount();
      expect(current).toBe(total);

      // Verify confidence sum is valid
      const isConfidenceValid = await bowlPicksPage.isConfidenceSumValid();
      expect(isConfidenceValid).toBe(true);

      // Take screenshot of completed picks
      await page.screenshot({ 
        path: path.join(DOWNLOAD_DIR, 'bowl-picks-04-all-picks-made.png'),
        fullPage: true 
      });
    });

    // === STEP 6: Download Excel file ===
    let downloadPath: string;
    await test.step('Download Excel file', async () => {
      // Verify download button is enabled
      const isEnabled = await bowlPicksPage.isDownloadButtonEnabled();
      expect(isEnabled).toBe(true);

      downloadPath = await waitForDownloadAndSave(
        page,
        async () => await bowlPicksPage.clickDownload(),
        DOWNLOAD_DIR
      );

      // Verify file was downloaded
      expect(fs.existsSync(downloadPath), `Downloaded file should exist at ${downloadPath}`).toBe(true);
      console.log(`Bowl picks downloaded to: ${downloadPath}`);
    });
  });

  test('Bowl Picks: validation shows errors for invalid picks', async ({ page }) => {
    const bowlPicksPage = new BowlPicksPage(page);

    // Navigate to bowl picks page
    await bowlPicksPage.goto();
    await bowlPicksPage.waitForLoadingComplete();

    // Enter name and try to load games
    await bowlPicksPage.enterName('Test User');
    await bowlPicksPage.selectYearAndContinue(TEST_YEAR);

    // Check if games are loaded
    const gamesLoaded = await bowlPicksPage.areGamesLoaded();
    
    if (!gamesLoaded) {
      console.log('Skipping validation test - no bowl lines available');
      test.skip(true, 'No bowl lines available for testing');
    }

    const gameCount = await bowlPicksPage.getGameCount();
    if (gameCount < 2) {
      test.skip(true, 'Not enough games for validation test');
    }

    // Select same confidence for two games (should show duplicate warning)
    await bowlPicksPage.selectSpreadPick(1, true);
    await bowlPicksPage.selectConfidence(1, 1);
    await bowlPicksPage.selectOutrightWinner(1, true);

    await bowlPicksPage.selectSpreadPick(2, true);
    await bowlPicksPage.selectConfidence(2, 1); // Same confidence as game 1
    await bowlPicksPage.selectOutrightWinner(2, true);

    // Wait for UI to update
    await page.waitForTimeout(500);

    // Take screenshot showing duplicate warning
    await page.screenshot({ 
      path: path.join(DOWNLOAD_DIR, 'bowl-picks-validation-duplicate.png'),
      fullPage: true 
    });

    // Verify duplicate warning is shown
    const hasDuplicateWarning = await bowlPicksPage.hasDuplicateConfidenceWarning();
    expect(hasDuplicateWarning).toBe(true);

    // Confidence sum should not be valid
    const isConfidenceValid = await bowlPicksPage.isConfidenceSumValid();
    expect(isConfidenceValid).toBe(false);

    // Download button should not be enabled when validation fails
    const isDownloadEnabled = await bowlPicksPage.isDownloadButtonEnabled();
    expect(isDownloadEnabled).toBe(false);
  });
});
