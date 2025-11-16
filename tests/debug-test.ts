import { test } from '@playwright/test';

test('debug - check page content', async ({ page }) => {
  await page.goto('http://localhost:5050');
  await page.waitForLoadState('networkidle');
  
  console.log('Page title:', await page.title());
  
  // Navigate to picks page
  const picksLink = page.locator('a[href="/picks"]').first();
  const count = await picksLink.count();
  console.log('Picks link count:', count);
  
  if (count > 0) {
    await picksLink.click();
    await page.waitForLoadState('networkidle');
  } else {
    await page.goto('http://localhost:5050/picks');
    await page.waitForLoadState('networkidle');
  }
  
  console.log('Current URL:', page.url());
  
  // Check what's on the page
  const bodyText = await page.locator('body').textContent();
  console.log('Body text (first 500 chars):', bodyText?.substring(0, 500));
  
  // Check for form elements
  const nameInput = page.locator('input#userName');
  console.log('Name input count:', await nameInput.count());
  
  const yearSelect = page.locator('select#year');
  console.log('Year select count:', await yearSelect.count());
  
  const weekSelect = page.locator('select#week');
  console.log('Week select count:', await weekSelect.count());
});
