import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for the Picks page
 * Handles navigation and user interactions for making weekly picks
 */
export class PicksPage {
  readonly page: Page;
  
  // Form elements
  readonly nameInput: Locator;
  readonly yearSelect: Locator;
  readonly weekSelect: Locator;
  readonly continueButton: Locator;
  
  // Game selection elements
  readonly backButton: Locator;
  readonly downloadButton: Locator;
  readonly printButton: Locator;
  
  // Status elements
  readonly loadingSpinner: Locator;
  readonly errorAlert: Locator;
  readonly pickCountDisplay: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Form elements
    this.nameInput = page.locator('#userName');
    this.yearSelect = page.locator('#year');
    this.weekSelect = page.locator('#week');
    this.continueButton = page.getByRole('button', { name: 'Continue to Picks' });
    
    // Game selection elements
    this.backButton = page.getByRole('button', { name: /← Back to Week Selection/ });
    this.downloadButton = page.getByRole('button', { name: /Generate Your Picks/ });
    this.printButton = page.getByRole('button', { name: /Print My Picks/ });
    
    // Status elements
    this.loadingSpinner = page.locator('.spinner-border');
    this.errorAlert = page.locator('.alert-danger');
    this.pickCountDisplay = page.locator('.alert-info');
  }

  /**
   * Navigate to the picks page
   */
  async goto(): Promise<void> {
    await this.page.goto('/picks');
    // Wait for the page to fully load
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Wait for loading to complete
   */
  async waitForLoadingComplete(): Promise<void> {
    // Wait for any loading spinners to disappear
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 });
  }

  /**
   * Enter the user's name
   */
  async enterName(name: string): Promise<void> {
    await this.nameInput.fill(name);
  }

  /**
   * Select year and week, then click continue
   */
  async selectWeek(year: number, week: number): Promise<void> {
    // Select year
    await this.yearSelect.selectOption(year.toString());
    
    // Wait for weeks to be loaded (wait for the week option to appear)
    await this.page.waitForFunction(
      (weekNum) => {
        const select = document.querySelector('#week') as HTMLSelectElement;
        if (!select) return false;
        return Array.from(select.options).some(opt => opt.value === weekNum.toString());
      },
      week,
      { timeout: 10000 }
    );
    
    // Select week
    await this.weekSelect.selectOption(week.toString());
    
    // Click continue
    await this.continueButton.click();
    
    // Wait for games to load
    await this.waitForLoadingComplete();
  }

  /**
   * Get all team selection buttons
   */
  async getTeamButtons(): Promise<Locator[]> {
    // Wait for buttons to appear
    await this.page.waitForSelector('.card .btn', { timeout: 10000 });
    
    // Get all buttons within game cards (both favorites and underdogs)
    const buttons = await this.page.locator('.card .d-grid .btn').all();
    return buttons;
  }

  /**
   * Select games by clicking on team buttons
   * @param count - Number of games to select (default: 6)
   */
  async selectGames(count: number = 6): Promise<void> {
    // Get all team buttons
    const buttons = await this.getTeamButtons();
    
    if (buttons.length === 0) {
      throw new Error('No team buttons found on the page');
    }

    // Wait for Blazor to be fully interactive by checking that the alert-info element exists
    // This ensures the page has finished rendering
    await this.page.waitForSelector('.alert-info', { timeout: 10000 });
    
    // Click on the first `count` buttons that are not disabled
    let selectedCount = 0;
    for (const button of buttons) {
      if (selectedCount >= count) break;
      
      // Check if button is not disabled
      const isDisabled = await button.isDisabled();
      if (!isDisabled) {
        // Use force click to ensure the click is registered even if element is being re-rendered
        await button.click({ force: true });
        selectedCount++;
        
        // Wait for the pick count display to update to reflect the selection
        // Use longer timeout for CI where Blazor WASM may be slower
        await this.page.waitForFunction(
          (expectedCount) => {
            const alertInfo = document.querySelector('.alert-info');
            if (!alertInfo) return false;
            const text = alertInfo.textContent || '';
            const match = text.match(/Selected:\s*(\d+)\s*\/\s*6/);
            return match && parseInt(match[1], 10) === expectedCount;
          },
          selectedCount,
          { timeout: 15000, polling: 100 }
        );
      }
    }
    
    if (selectedCount < count) {
      throw new Error(`Could only select ${selectedCount} games, needed ${count}`);
    }
  }

  /**
   * Click the download picks button
   */
  async clickDownload(): Promise<void> {
    await this.downloadButton.click();
  }

  /**
   * Get the current number of selected picks
   */
  async getSelectedPickCount(): Promise<number> {
    const text = await this.pickCountDisplay.textContent();
    const match = text?.match(/Selected:\s*(\d+)\s*\/\s*6/);
    return match ? parseInt(match[1], 10) : 0;
  }

  /**
   * Check if download button is enabled
   */
  async isDownloadButtonEnabled(): Promise<boolean> {
    // First check if button is visible
    const isVisible = await this.downloadButton.isVisible().catch(() => false);
    if (!isVisible) return false;
    
    // Then check if it's enabled
    return !(await this.downloadButton.isDisabled());
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
}
