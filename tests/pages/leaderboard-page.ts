import { Locator, Page } from '@playwright/test';

/**
 * Page Object Model for the Leaderboard page
 * Handles navigation and interactions for viewing standings
 */
export class LeaderboardPage {
    readonly page: Page;

    // Selectors
    readonly yearSelect: Locator;
    readonly viewSelect: Locator;
    readonly weekSelect: Locator;

    // Loading and error states
    readonly loadingSpinner: Locator;
    readonly errorAlert: Locator;
    readonly noDataAlert: Locator;

    // Season standings table
    readonly seasonTable: Locator;
    readonly seasonTableRows: Locator;

    // Weekly standings table
    readonly weeklyTable: Locator;
    readonly weeklyTableRows: Locator;

    // Badges
    readonly perfectBadge: Locator;
    readonly rankBadges: Locator;

    constructor(page: Page) {
        this.page = page;

        // Selectors
        this.yearSelect = page.locator('#yearSelect');
        this.viewSelect = page.locator('#viewSelect');
        this.weekSelect = page.locator('#weekSelect');

        // Loading and error states
        this.loadingSpinner = page.locator('.spinner-border');
        this.errorAlert = page.locator('.alert-danger');
        this.noDataAlert = page.locator('.alert-info');

        // Tables
        this.seasonTable = page.locator('table').first();
        this.seasonTableRows = page.locator('table tbody tr');
        this.weeklyTable = page.locator('table').first();
        this.weeklyTableRows = page.locator('table tbody tr');

        // Badges
        this.perfectBadge = page.locator('.badge:has-text("PERFECT")');
        this.rankBadges = page.locator('td .badge');
    }

    /**
     * Navigate to the leaderboard page
     */
    async goto(): Promise<void> {
        await this.page.goto('/leaderboard');
        await this.page.waitForLoadState('networkidle');
    }

    /**
     * Wait for loading to complete
     */
    async waitForLoadingComplete(): Promise<void> {
        await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 });
    }

    /**
     * Select a year from the dropdown
     */
    async selectYear(year: number): Promise<void> {
        await this.yearSelect.selectOption(year.toString());
        await this.waitForLoadingComplete();
    }

    /**
     * Select view type (season or weekly)
     */
    async selectView(view: 'season' | 'weekly'): Promise<void> {
        await this.viewSelect.selectOption(view);
        await this.waitForLoadingComplete();
    }

    /**
     * Select a week (only available in weekly view)
     */
    async selectWeek(week: number): Promise<void> {
        await this.weekSelect.selectOption(week.toString());
        await this.waitForLoadingComplete();
    }

    /**
     * Get the number of rows in the standings table
     */
    async getRowCount(): Promise<number> {
        await this.waitForLoadingComplete();
        return await this.seasonTableRows.count();
    }

    /**
     * Check if standings table is visible
     */
    async hasStandings(): Promise<boolean> {
        await this.waitForLoadingComplete();
        const tableVisible = await this.seasonTable.isVisible().catch(() => false);
        const rowCount = await this.getRowCount();
        return tableVisible && rowCount > 0;
    }

    /**
     * Check if no data message is shown
     */
    async hasNoDataMessage(): Promise<boolean> {
        return await this.noDataAlert.isVisible().catch(() => false);
    }

    /**
     * Check if error is displayed
     */
    async hasError(): Promise<boolean> {
        return await this.errorAlert.isVisible().catch(() => false);
    }

    /**
     * Get player name from a specific row (1-indexed)
     */
    async getPlayerNameFromRow(rowIndex: number): Promise<string> {
        const row = this.seasonTableRows.nth(rowIndex - 1);
        const nameCell = row.locator('td').nth(1).locator('a');
        return await nameCell.textContent() || '';
    }

    /**
     * Click on a player name to view their history
     */
    async clickPlayerName(playerName: string): Promise<void> {
        const link = this.page.locator(`a:has-text("${playerName}")`).first();
        await link.click();
        await this.page.waitForLoadState('networkidle');
    }

    /**
     * Get wins count from a specific row (1-indexed)
     */
    async getWinsFromRow(rowIndex: number): Promise<number> {
        const row = this.seasonTableRows.nth(rowIndex - 1);
        const winsCell = row.locator('td.text-success').first();
        const text = await winsCell.textContent() || '0';
        return parseInt(text, 10);
    }

    /**
     * Get losses count from a specific row (1-indexed)
     */
    async getLossesFromRow(rowIndex: number): Promise<number> {
        const row = this.seasonTableRows.nth(rowIndex - 1);
        const lossesCell = row.locator('td.text-danger').first();
        const text = await lossesCell.textContent() || '0';
        return parseInt(text, 10);
    }

    /**
     * Check if a player has a PERFECT badge in weekly view
     */
    async hasPerfectBadge(): Promise<boolean> {
        return await this.perfectBadge.isVisible().catch(() => false);
    }

    /**
     * Get the rank badge text for a row (1st, 2nd, 3rd)
     */
    async getRankBadgeText(rowIndex: number): Promise<string | null> {
        const row = this.seasonTableRows.nth(rowIndex - 1);
        const badge = row.locator('td .badge').first();
        if (await badge.isVisible()) {
            return await badge.textContent();
        }
        return null;
    }
}
