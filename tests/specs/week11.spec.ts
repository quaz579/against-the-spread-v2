import { test } from '@playwright/test';
import { testWeekFlow } from '../helpers/test-flow';

/**
 * Smoke test for Week 11 complete user flow
 */
test.describe('Week 11 Smoke Tests', () => {
  test('should complete full user flow for Week 11', async ({ page }) => {
    await testWeekFlow(page, 11, 'Test User');
  });
});
