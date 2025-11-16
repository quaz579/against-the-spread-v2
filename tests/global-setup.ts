import { TestEnvironment } from './helpers/test-environment';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Global setup for Playwright tests
 * Starts Azurite, Functions, and Web App
 * Saves environment info to a state file for tests to use
 */
export default async function globalSetup() {
  console.log('=== Starting Global Setup ===');
  
  const repoRoot = path.resolve(__dirname, '..');
  const testEnv = new TestEnvironment(repoRoot);
  
  try {
    // Start services
    await testEnv.startAzurite();
    await testEnv.startFunctions();
    await testEnv.startWebApp();
    
    // Upload test data
    const referenceDocsPath = path.join(repoRoot, 'reference-docs');
    await testEnv.uploadLinesFile(
      path.join(referenceDocsPath, 'Week 11 Lines.xlsx'),
      11,
      2025
    );
    await testEnv.uploadLinesFile(
      path.join(referenceDocsPath, 'Week 12 Lines.xlsx'),
      12,
      2025
    );
    
    // Save environment state for tests to use (optional, for reference)
    const stateFile = path.join(__dirname, '.test-state.json');
    fs.writeFileSync(stateFile, JSON.stringify({
      webUrl: testEnv.webUrl,
      functionsUrl: testEnv.functionsUrl,
      setupComplete: true,
      timestamp: new Date().toISOString()
    }, null, 2));
    
    console.log('=== Global Setup Complete ===');
    console.log('Note: Cleanup skipped - running on ephemeral CI runner');
  } catch (error) {
    console.error('Global setup failed:', error);
    throw error;
  }
}
