import { Page, Download } from '@playwright/test';
import * as path from 'path';
import * as fs from 'fs';
import * as os from 'os';

/**
 * Get the default download directory for Playwright tests
 * Uses os.tmpdir() for cross-platform compatibility
 */
export function getDefaultDownloadDir(): string {
  return path.join(os.tmpdir(), 'playwright-downloads');
}

/**
 * Wait for a download to complete and save it to a specified directory
 * 
 * @param page - Playwright page instance
 * @param downloadTriggerAction - Async function that triggers the download (e.g., clicking a button)
 * @param saveDir - Directory to save the downloaded file (default: cross-platform temp directory)
 * @returns Full path to the saved file
 */
export async function waitForDownloadAndSave(
  page: Page,
  downloadTriggerAction: () => Promise<void>,
  saveDir: string = getDefaultDownloadDir()
): Promise<string> {
  // Ensure the save directory exists
  if (!fs.existsSync(saveDir)) {
    fs.mkdirSync(saveDir, { recursive: true });
  }
  
  // Set up download event listener before triggering the download
  const downloadPromise = page.waitForEvent('download');
  
  // Trigger the download action
  await downloadTriggerAction();
  
  // Wait for the download to start
  const download: Download = await downloadPromise;
  
  // Get the suggested filename
  const suggestedFilename = download.suggestedFilename();
  
  // Generate the full save path
  const savePath = path.join(saveDir, suggestedFilename);
  
  // Save the download to the specified path
  await download.saveAs(savePath);
  
  // Verify the file exists
  if (!fs.existsSync(savePath)) {
    throw new Error(`Download failed: file not found at ${savePath}`);
  }
  
  return savePath;
}

/**
 * Clean up downloaded files in a directory
 * 
 * @param dir - Directory to clean up (default: cross-platform temp directory)
 */
export function cleanupDownloads(dir: string = getDefaultDownloadDir()): void {
  if (fs.existsSync(dir)) {
    const files = fs.readdirSync(dir);
    for (const file of files) {
      fs.unlinkSync(path.join(dir, file));
    }
  }
}
