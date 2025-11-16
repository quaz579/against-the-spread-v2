import { test } from '@playwright/test';
import { testWeekFlow } from '../helpers/test-flow';

/**
 * Parameterized smoke tests for multiple weeks
 * Tests the complete user flow for each week
 */
test.describe('Weekly Smoke Tests', () => {
  const weekTestCases = [
    { week: 11, userName: 'Test User' },
    { week: 12, userName: 'Test User Week 12' }
  ];

  for (const { week, userName } of weekTestCases) {
    test(`should complete full user flow for Week ${week}`, async ({ page }) => {
      await testWeekFlow(page, week, userName);
    });
  }
});
