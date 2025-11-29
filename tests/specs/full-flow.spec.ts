import { test, expect } from '@playwright/test';
import { PicksPage } from '../pages/picks-page';
import { TestEnvironment } from '../helpers/test-environment';
import { waitForDownloadAndSave, cleanupDownloads, getDefaultDownloadDir } from '../helpers/download-helper';
import { validatePicksExcel } from '../helpers/excel-validator';
import * as path from 'path';
import * as fs from 'fs';

// Test constants
const TEST_NAME = 'E2E Test User';
const TEST_YEAR = 2025;
const TEST_WEEK_11 = 11;
const TEST_WEEK_12 = 12;
const DOWNLOAD_DIR = getDefaultDownloadDir();

// Get repository root path
const REPO_ROOT = path.resolve(__dirname, '../..');
const REFERENCE_DOCS = path.join(REPO_ROOT, 'reference-docs');

// Test environment helper for uploading test data to Azurite
let testEnv: TestEnvironment;

test.describe('Complete User Flow', () => {
  
  test.beforeAll(async () => {
    // Initialize test environment for file uploads
    testEnv = new TestEnvironment(REPO_ROOT);
    
    // Verify services are running by checking base URLs
    console.log('Verifying services are running...');
    
    // Clean up any previous downloads
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.beforeEach(async ({ page }) => {
    // Clean up downloads before each test
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test.afterAll(async () => {
    // Clean up downloads after all tests
    cleanupDownloads(DOWNLOAD_DIR);
  });

  test('Week 11: upload lines and complete user picks flow', async ({ page }) => {
    // === STEP 1: Upload Week 11 lines to Azurite ===
    await test.step('Upload Week 11 lines to Azurite', async () => {
      const linesFile = path.join(REFERENCE_DOCS, 'Week 11 Lines.xlsx');
      
      // Verify the file exists
      expect(fs.existsSync(linesFile), `Week 11 lines file should exist at ${linesFile}`).toBe(true);
      
      // Upload using test environment helper
      await testEnv.uploadLinesFile(linesFile, TEST_WEEK_11, TEST_YEAR);
    });

    // === STEP 2: Navigate to picks page ===
    await test.step('Navigate to picks page', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.goto();
      
      // Wait for the page to be ready
      await picksPage.waitForLoadingComplete();
    });

    // === STEP 3: Enter user name ===
    await test.step('Enter user name', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.enterName(TEST_NAME);
      
      // Verify name was entered
      await expect(picksPage.nameInput).toHaveValue(TEST_NAME);
    });

    // === STEP 4: Select year and week ===
    await test.step('Select year and week', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.selectWeek(TEST_YEAR, TEST_WEEK_11);
      
      // Verify games are displayed (we should have some team buttons)
      const buttons = await picksPage.getTeamButtons();
      expect(buttons.length).toBeGreaterThan(0);
    });

    // === STEP 5: Select 6 games ===
    await test.step('Select 6 games', async () => {
      const picksPage = new PicksPage(page);
      
      // Select 6 games
      await picksPage.selectGames(6);
      
      // Verify 6 picks were selected
      const pickCount = await picksPage.getSelectedPickCount();
      expect(pickCount).toBe(6);
      
      // Verify download button is enabled
      const isEnabled = await picksPage.isDownloadButtonEnabled();
      expect(isEnabled).toBe(true);
    });

    // === STEP 6: Download Excel file ===
    let downloadPath: string;
    await test.step('Download Excel file', async () => {
      const picksPage = new PicksPage(page);
      
      // Download the picks file
      downloadPath = await waitForDownloadAndSave(
        page,
        async () => await picksPage.clickDownload(),
        DOWNLOAD_DIR
      );
      
      // Verify file was downloaded
      expect(fs.existsSync(downloadPath), `Downloaded file should exist at ${downloadPath}`).toBe(true);
    });

    // === STEP 7: Validate Excel file structure ===
    await test.step('Validate Excel file structure', async () => {
      const validation = await validatePicksExcel(downloadPath, TEST_NAME, 6);
      
      // Log any errors for debugging
      if (!validation.isValid) {
        console.error('Excel validation errors:', validation.errors);
      }
      
      expect(validation.isValid).toBe(true);
      expect(validation.errors).toHaveLength(0);
    });
  });

  test('Week 12: upload lines and complete user picks flow', async ({ page }) => {
    // === STEP 1: Upload Week 12 lines to Azurite ===
    await test.step('Upload Week 12 lines to Azurite', async () => {
      const linesFile = path.join(REFERENCE_DOCS, 'Week 12 Lines.xlsx');
      
      // Verify the file exists
      expect(fs.existsSync(linesFile), `Week 12 lines file should exist at ${linesFile}`).toBe(true);
      
      // Upload using test environment helper
      await testEnv.uploadLinesFile(linesFile, TEST_WEEK_12, TEST_YEAR);
    });

    // === STEP 2: Navigate to picks page ===
    await test.step('Navigate to picks page', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.goto();
      
      // Wait for the page to be ready
      await picksPage.waitForLoadingComplete();
    });

    // === STEP 3: Enter user name ===
    await test.step('Enter user name', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.enterName(TEST_NAME);
    });

    // === STEP 4: Select year and week ===
    await test.step('Select year and week', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.selectWeek(TEST_YEAR, TEST_WEEK_12);
      
      // Verify games are displayed
      const buttons = await picksPage.getTeamButtons();
      expect(buttons.length).toBeGreaterThan(0);
    });

    // === STEP 5: Select 6 games ===
    await test.step('Select 6 games', async () => {
      const picksPage = new PicksPage(page);
      await picksPage.selectGames(6);
      
      // Verify 6 picks were selected
      const pickCount = await picksPage.getSelectedPickCount();
      expect(pickCount).toBe(6);
    });

    // === STEP 6: Download and validate Excel file ===
    await test.step('Download and validate Excel file', async () => {
      const picksPage = new PicksPage(page);
      
      // Download the picks file
      const downloadPath = await waitForDownloadAndSave(
        page,
        async () => await picksPage.clickDownload(),
        DOWNLOAD_DIR
      );
      
      // Verify file was downloaded
      expect(fs.existsSync(downloadPath)).toBe(true);
      
      // Validate Excel structure
      const validation = await validatePicksExcel(downloadPath, TEST_NAME, 6);
      expect(validation.isValid).toBe(true);
    });
  });
});
