import { Locator, Page } from '@playwright/test';

/** Timeout for modal operations in milliseconds */
const MODAL_TIMEOUT = 5000;

/**
 * Page Object Model for the Bowl Picks page
 * Handles navigation and user interactions for making bowl game picks
 */
export class BowlPicksPage {
  readonly page: Page;

  // Form elements (initial entry)
  readonly nameInput: Locator;
  readonly yearSelect: Locator;
  readonly continueButton: Locator;
  readonly backButton: Locator;

  // Status elements
  readonly loadingSpinner: Locator;
  readonly errorAlert: Locator;
  readonly progressAlert: Locator;
  readonly picksCountDisplay: Locator;
  readonly confidenceSumDisplay: Locator;

  // Game selection elements (each game has spread pick, confidence dropdown, and outright winner)
  readonly gameCards: Locator;
  readonly downloadButton: Locator;

  constructor(page: Page) {
    this.page = page;

    // Form elements
    this.nameInput = page.locator('#userName');
    this.yearSelect = page.locator('#year');
    this.continueButton = page.getByRole('button', { name: 'Continue to Bowl Picks' });
    this.backButton = page.getByRole('button', { name: /← Back to Selection/ });

    // Status elements
    this.loadingSpinner = page.locator('.spinner-border');
    this.errorAlert = page.locator('.alert-danger');
    this.progressAlert = page.locator('.alert-info, .alert-success').first();
    this.picksCountDisplay = page.locator('text=Picks Made:');
    this.confidenceSumDisplay = page.locator('text=Confidence Sum:');

    // Game selection elements
    this.gameCards = page.locator('.card').filter({ has: page.locator('.card-header') });
    this.downloadButton = page.getByRole('button', { name: /Generate Bowl Picks Excel/ });
  }

  /**
   * Get the locator for a confidence row in the modal
   * @param confidence - The confidence value to find
   */
  private getConfidenceRowLocator(confidence: number): Locator {
    return this.page.locator(`.list-group-item:has(.confidence-badge-modal:text-is("${confidence}"))`);
  }

  /**
   * Navigate to the bowl picks page
   */
  async goto(): Promise<void> {
    await this.page.goto('/bowl-picks');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Wait for loading to complete
   */
  async waitForLoadingComplete(): Promise<void> {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 });
  }

  /**
   * Enter the user's name
   */
  async enterName(name: string): Promise<void> {
    await this.nameInput.fill(name);
    // Trigger blur to ensure Blazor binding updates
    await this.nameInput.blur();
    await this.page.waitForTimeout(200); // Wait for Blazor to process the binding
  }

  /**
   * Select year and click continue to load bowl games
   */
  async selectYearAndContinue(year: number): Promise<void> {
    await this.yearSelect.selectOption(year.toString());
    // Wait for button to be enabled (Blazor binding delay)
    await this.continueButton.waitFor({ state: 'visible', timeout: 5000 });
    await this.page.waitForTimeout(500); // Additional wait for Blazor to update button state
    await this.continueButton.click({ force: false, timeout: 15000 });
    await this.waitForLoadingComplete();
  }

  /**
   * Check if bowl games are loaded (games are displayed)
   */
  async areGamesLoaded(): Promise<boolean> {
    const cardCount = await this.gameCards.count();
    return cardCount > 0;
  }

  /**
   * Get the number of bowl games displayed
   */
  async getGameCount(): Promise<number> {
    return await this.gameCards.count();
  }

  /**
   * Select spread pick for a specific game (1-indexed)
   * @param gameNumber - Game number (1-indexed)
   * @param pickFavorite - true to pick favorite, false to pick underdog
   */
  async selectSpreadPick(gameNumber: number, pickFavorite: boolean): Promise<void> {
    const gameCard = this.gameCards.nth(gameNumber - 1);
    const spreadButtons = gameCard.locator('label:has-text("Spread Pick") ~ .btn-group-vertical button');
    
    if (pickFavorite) {
      await spreadButtons.first().click();
    } else {
      await spreadButtons.last().click();
    }
  }

  /**
   * Select confidence points for a specific game using the modal
   * @param gameNumber - Game number (1-indexed)
   * @param confidence - Confidence points to assign
   */
  async selectConfidence(gameNumber: number, confidence: number): Promise<void> {
    const gameCard = this.gameCards.nth(gameNumber - 1);
    const confidenceBtn = gameCard.locator('.confidence-btn');
    
    // Click the confidence button to open modal
    await confidenceBtn.click();
    
    // Wait for modal to appear
    await this.page.waitForSelector('.modal.show', { timeout: MODAL_TIMEOUT });
    
    // Find the row with the target confidence and click MOVE HERE
    const targetRow = this.getConfidenceRowLocator(confidence);
    const moveHereButton = targetRow.locator('button:text("MOVE HERE")');
    
    // Check if the button exists and is visible
    if (await moveHereButton.isVisible()) {
      await moveHereButton.click();
    } else {
      // MOVE HERE button may not be visible if:
      // - This is the currently selected game (shows "SELECTED" badge instead)
      // - The game with this confidence is locked (shows "LOCKED" badge)
      // In either case, just close the modal without making changes
      await this.page.locator('.modal .btn-close').click();
    }
    
    // Wait for modal to close
    await this.page.waitForSelector('.modal.show', { state: 'hidden', timeout: MODAL_TIMEOUT });
  }

  /**
   * Select outright winner for a specific game
   * @param gameNumber - Game number (1-indexed)
   * @param pickFavorite - true to pick favorite, false to pick underdog
   */
  async selectOutrightWinner(gameNumber: number, pickFavorite: boolean): Promise<void> {
    const gameCard = this.gameCards.nth(gameNumber - 1);
    const outrightButtons = gameCard.locator('label:has-text("Outright Winner") ~ .btn-group-vertical button');
    
    if (pickFavorite) {
      await outrightButtons.first().click();
    } else {
      await outrightButtons.last().click();
    }
  }

  /**
   * Make complete picks for all games
   * Each game gets: spread pick (favorite), unique confidence, and outright winner (favorite)
   * @param totalGames - Number of games to make picks for
   */
  async makeAllPicks(totalGames: number): Promise<void> {
    for (let i = 1; i <= totalGames; i++) {
      // Select spread pick (alternate between favorite and underdog)
      await this.selectSpreadPick(i, i % 2 === 1);
      
      // Select unique confidence points (1 to totalGames)
      await this.selectConfidence(i, i);
      
      // Select outright winner (same as spread pick for simplicity)
      await this.selectOutrightWinner(i, i % 2 === 1);
      
      // Small delay to let Blazor update
      await this.page.waitForTimeout(100);
    }
  }

  /**
   * Get the current confidence sum from the display
   */
  async getCurrentConfidenceSum(): Promise<{ current: number; expected: number }> {
    const text = await this.progressAlert.textContent() || '';
    const match = text.match(/Confidence Sum:\s*(\d+)\s*\/\s*(\d+)/);
    
    if (match) {
      return {
        current: parseInt(match[1], 10),
        expected: parseInt(match[2], 10)
      };
    }
    return { current: 0, expected: 0 };
  }

  /**
   * Get the completed picks count from the display
   */
  async getCompletedPicksCount(): Promise<{ current: number; total: number }> {
    const text = await this.progressAlert.textContent() || '';
    const match = text.match(/Picks Made:\s*(\d+)\s*\/\s*(\d+)/);
    
    if (match) {
      return {
        current: parseInt(match[1], 10),
        total: parseInt(match[2], 10)
      };
    }
    return { current: 0, total: 0 };
  }

  /**
   * Check if download button is visible and enabled
   */
  async isDownloadButtonEnabled(): Promise<boolean> {
    const isVisible = await this.downloadButton.isVisible().catch(() => false);
    if (!isVisible) return false;
    return !(await this.downloadButton.isDisabled());
  }

  /**
   * Click the download button
   */
  async clickDownload(): Promise<void> {
    await this.downloadButton.click();
  }

  /**
   * Check if an error is displayed
   */
  async hasError(): Promise<boolean> {
    return await this.errorAlert.isVisible().catch(() => false);
  }

  /**
   * Get error message text
   */
  async getErrorMessage(): Promise<string> {
    if (await this.hasError()) {
      return await this.errorAlert.textContent() || '';
    }
    return '';
  }

  /**
   * Check if confidence sum is valid (green checkmark)
   */
  async isConfidenceSumValid(): Promise<boolean> {
    const validIndicator = this.page.locator('.text-success:has-text("✓")');
    return await validIndicator.isVisible().catch(() => false);
  }

  /**
   * Check if there are duplicate confidence warnings
   */
  async hasDuplicateConfidenceWarning(): Promise<boolean> {
    const warning = this.page.locator('text=Duplicate confidence');
    return await warning.isVisible().catch(() => false);
  }

  /**
   * Check if a confidence value is assigned to a locked game
   * @param confidence - Confidence points value to check
   * @returns true if the game with this confidence is locked, false otherwise
   */
  async isConfidenceLocked(confidence: number): Promise<boolean> {
    // Open modal to check (we'll need to find a game to click)
    const firstGameBtn = this.gameCards.first().locator('.confidence-btn');
    await firstGameBtn.click();
    await this.page.waitForSelector('.modal.show', { timeout: MODAL_TIMEOUT });
    
    // Find the row with this confidence
    const targetRow = this.getConfidenceRowLocator(confidence);
    const lockedBadge = targetRow.locator('.badge:text("LOCKED")');
    const isLocked = await lockedBadge.isVisible();
    
    // Close modal
    await this.page.locator('.modal .btn-secondary').click();
    await this.page.waitForSelector('.modal.show', { state: 'hidden', timeout: MODAL_TIMEOUT });
    
    return isLocked;
  }

  /**
   * Get the current confidence value displayed for a specific game
   * @param gameNumber - Game number (1-indexed)
   * @returns The confidence value or 0 if not set
   */
  async getConfidenceValue(gameNumber: number): Promise<number> {
    const gameCard = this.gameCards.nth(gameNumber - 1);
    const confidenceBtn = gameCard.locator('.confidence-btn');
    const text = await confidenceBtn.textContent() || '';
    const match = text.match(/(\d+)/);
    return match ? parseInt(match[1], 10) : 0;
  }

  /**
   * @deprecated Use getConfidenceValue instead - modal interface doesn't use disabled options
   */
  async isConfidenceOptionDisabled(gameNumber: number, confidence: number): Promise<boolean> {
    console.warn('isConfidenceOptionDisabled is deprecated - modal interface uses LOCKED badges');
    return false;
  }

  /**
   * @deprecated Use isConfidenceLocked instead - modal interface doesn't use disabled options
   */
  async getDisabledConfidenceOptions(gameNumber: number): Promise<number[]> {
    console.warn('getDisabledConfidenceOptions is deprecated - modal interface uses LOCKED badges');
    return [];
  }

  /**
   * @deprecated Modal interface shows confidence as badges, not dropdown options
   */
  async getConfidenceOptionText(gameNumber: number, confidence: number): Promise<string> {
    console.warn('getConfidenceOptionText is deprecated - modal interface uses badges');
    return confidence.toString();
  }
}
