import { test } from '@playwright/test';
import { testWeekFlow } from '../helpers/test-flow';

/**
 * Smoke test for Week 12 complete user flow
 */
test.describe('Week 12 Smoke Tests', () => {
  test('should complete full user flow for Week 12', async ({ page }) => {
    await testWeekFlow(page, 12, 'Test User Week 12');
  });
});
