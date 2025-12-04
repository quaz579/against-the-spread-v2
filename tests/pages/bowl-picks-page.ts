import { Locator, Page } from '@playwright/test';

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
  }

  /**
   * Select year and click continue to load bowl games
   */
  async selectYearAndContinue(year: number): Promise<void> {
    await this.yearSelect.selectOption(year.toString());
    await this.continueButton.click();
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
   * Select confidence points for a specific game
   * @param gameNumber - Game number (1-indexed)
   * @param confidence - Confidence points to assign
   */
  async selectConfidence(gameNumber: number, confidence: number): Promise<void> {
    const gameCard = this.gameCards.nth(gameNumber - 1);
    const confidenceSelect = gameCard.locator('select.form-select');
    await confidenceSelect.selectOption(confidence.toString());
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
}
