import { chromium } from 'playwright';
import * as path from 'path';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    console.log('Navigating to bowl picks page...');
    await page.goto('http://localhost:5158/bowl-picks');
    await page.waitForLoadState('networkidle');
    
    // Take screenshot of initial page
    await page.screenshot({ path: '/tmp/bowl-picks-01-initial.png', fullPage: true });
    console.log('Screenshot saved: bowl-picks-01-initial.png');

    // Enter name
    await page.fill('#userName', 'Test User');
    await page.selectOption('#year', '2025');
    await page.screenshot({ path: '/tmp/bowl-picks-02-name-entered.png', fullPage: true });
    console.log('Screenshot saved: bowl-picks-02-name-entered.png');

    // Click continue (if bowl lines are available)
    const continueButton = page.getByRole('button', { name: 'Continue to Bowl Picks' });
    if (await continueButton.isVisible()) {
      await continueButton.click();
      await page.waitForTimeout(2000);
      
      // Check if games loaded or error
      const hasError = await page.locator('.alert-danger').isVisible().catch(() => false);
      if (hasError) {
        console.log('No bowl lines available - this is expected');
        await page.screenshot({ path: '/tmp/bowl-picks-03-no-lines.png', fullPage: true });
        console.log('Screenshot saved: bowl-picks-03-no-lines.png');
      } else {
        // Games loaded - test the confidence dropdown
        const gameCards = page.locator('.card').filter({ has: page.locator('.card-header') });
        const gameCount = await gameCards.count();
        console.log(`Found ${gameCount} games`);
        
        if (gameCount > 0) {
          await page.screenshot({ path: '/tmp/bowl-picks-04-games-loaded.png', fullPage: true });
          console.log('Screenshot saved: bowl-picks-04-games-loaded.png');
          
          // Select confidence for first game
          const game1Card = gameCards.nth(0);
          const game1Confidence = game1Card.locator('select.form-select');
          await game1Confidence.selectOption('1');
          await page.waitForTimeout(500);
          
          // Take screenshot showing first confidence selected
          await page.screenshot({ path: '/tmp/bowl-picks-05-first-confidence.png', fullPage: true });
          console.log('Screenshot saved: bowl-picks-05-first-confidence.png');
          
          // Try to select same confidence for second game - should be disabled
          const game2Card = gameCards.nth(1);
          const game2Confidence = game2Card.locator('select.form-select');
          
          // Get all options and check if option 1 is disabled
          const option1InGame2 = game2Confidence.locator('option[value="1"]');
          const isDisabled = await option1InGame2.getAttribute('disabled');
          console.log(`Option 1 in game 2 disabled: ${isDisabled !== null}`);
          
          // Scroll down to see second game
          await game2Card.scrollIntoViewIfNeeded();
          await page.waitForTimeout(500);
          await page.screenshot({ path: '/tmp/bowl-picks-06-second-game-option-disabled.png', fullPage: true });
          console.log('Screenshot saved: bowl-picks-06-second-game-option-disabled.png');
          
          // Select confidence 2 for game 2
          await game2Confidence.selectOption('2');
          await page.waitForTimeout(500);
          
          // Now option 2 should be disabled in game 1
          const option2InGame1 = game1Confidence.locator('option[value="2"]');
          const isOption2Disabled = await option2InGame1.getAttribute('disabled');
          console.log(`Option 2 in game 1 disabled: ${isOption2Disabled !== null}`);
          
          await game1Card.scrollIntoViewIfNeeded();
          await page.waitForTimeout(500);
          await page.screenshot({ path: '/tmp/bowl-picks-07-option-2-disabled-in-game1.png', fullPage: true });
          console.log('Screenshot saved: bowl-picks-07-option-2-disabled-in-game1.png');
        }
      }
    }

    console.log('Test completed successfully!');
  } catch (error) {
    console.error('Error during test:', error);
    await page.screenshot({ path: '/tmp/bowl-picks-error.png', fullPage: true });
  } finally {
    await browser.close();
  }
})();
