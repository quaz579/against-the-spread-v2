import { Locator, Page } from '@playwright/test';

/**
 * Page Object Model for the My Picks page
 * Handles navigation and interactions for viewing user's pick history
 */
export class MyPicksPage {
    readonly page: Page;

    // Auth elements
    readonly signInPrompt: Locator;
    readonly signInButton: Locator;

    // Loading and error states
    readonly loadingSpinner: Locator;
    readonly errorAlert: Locator;
    readonly noPicksAlert: Locator;

    // User info
    readonly welcomeMessage: Locator;
    readonly yearSelect: Locator;

    // Season summary cards
    readonly winsCard: Locator;
    readonly lossesCard: Locator;
    readonly pushesCard: Locator;
    readonly winRateCard: Locator;

    // Week cards
    readonly weekCards: Locator;
    readonly weekHeaders: Locator;

    // Result badges
    readonly winBadges: Locator;
    readonly lossBadges: Locator;
    readonly pushBadges: Locator;
    readonly pendingBadges: Locator;
    readonly perfectWeekBadge: Locator;

    constructor(page: Page) {
        this.page = page;

        // Auth elements
        this.signInPrompt = page.locator('.alert-info:has-text("Sign in to view your picks")');
        this.signInButton = page.getByRole('button', { name: 'Sign In' });

        // Loading and error states
        this.loadingSpinner = page.locator('.spinner-border');
        this.errorAlert = page.locator('.alert-danger');
        this.noPicksAlert = page.locator('.alert-info:has-text("No picks yet")');

        // User info
        this.welcomeMessage = page.locator('p.text-muted:has-text("Welcome")');
        this.yearSelect = page.locator('select.form-select');

        // Season summary cards
        this.winsCard = page.locator('.card.text-bg-success');
        this.lossesCard = page.locator('.card.text-bg-danger');
        this.pushesCard = page.locator('.card.text-bg-secondary');
        this.winRateCard = page.locator('.card.text-bg-primary');

        // Week cards
        this.weekCards = page.locator('.card.mb-3');
        this.weekHeaders = page.locator('.card-header');

        // Result badges
        this.winBadges = page.locator('.badge.bg-success:has-text("WIN")');
        this.lossBadges = page.locator('.badge.bg-danger:has-text("LOSS")');
        this.pushBadges = page.locator('.badge.bg-secondary:has-text("PUSH")');
        this.pendingBadges = page.locator('.badge.bg-secondary:has-text("Pending")');
        this.perfectWeekBadge = page.locator('.badge:has-text("PERFECT WEEK")');
    }

    /**
     * Navigate to the my picks page
     */
    async goto(): Promise<void> {
        await this.page.goto('/my-picks');
        await this.page.waitForLoadState('networkidle');
    }

    /**
     * Wait for loading to complete
     */
    async waitForLoadingComplete(): Promise<void> {
        await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 30000 });
    }

    /**
     * Check if sign-in prompt is shown (unauthenticated)
     */
    async requiresAuthentication(): Promise<boolean> {
        return await this.signInPrompt.isVisible().catch(() => false);
    }

    /**
     * Click sign in button
     */
    async clickSignIn(): Promise<void> {
        await this.signInButton.click();
    }

    /**
     * Check if user has picks displayed
     */
    async hasPicks(): Promise<boolean> {
        await this.waitForLoadingComplete();
        const weekCardsCount = await this.weekCards.count();
        return weekCardsCount > 0;
    }

    /**
     * Check if no picks message is shown
     */
    async hasNoPicksMessage(): Promise<boolean> {
        return await this.noPicksAlert.isVisible().catch(() => false);
    }

    /**
     * Get the total wins displayed in the summary card
     */
    async getTotalWins(): Promise<number> {
        const text = await this.winsCard.locator('h3').textContent() || '0';
        return parseInt(text, 10);
    }

    /**
     * Get the total losses displayed in the summary card
     */
    async getTotalLosses(): Promise<number> {
        const text = await this.lossesCard.locator('h3').textContent() || '0';
        return parseInt(text, 10);
    }

    /**
     * Get the total pushes displayed in the summary card
     */
    async getTotalPushes(): Promise<number> {
        const text = await this.pushesCard.locator('h3').textContent() || '0';
        return parseInt(text, 10);
    }

    /**
     * Get the win rate percentage displayed
     */
    async getWinRate(): Promise<string> {
        const text = await this.winRateCard.locator('h3').textContent() || '0%';
        return text.trim();
    }

    /**
     * Get the number of week cards displayed
     */
    async getWeekCount(): Promise<number> {
        return await this.weekCards.count();
    }

    /**
     * Get WIN badge count across all weeks
     */
    async getWinBadgeCount(): Promise<number> {
        return await this.winBadges.count();
    }

    /**
     * Get LOSS badge count across all weeks
     */
    async getLossBadgeCount(): Promise<number> {
        return await this.lossBadges.count();
    }

    /**
     * Get PUSH badge count across all weeks
     */
    async getPushBadgeCount(): Promise<number> {
        return await this.pushBadges.count();
    }

    /**
     * Check if any perfect week badges are displayed
     */
    async hasPerfectWeekBadge(): Promise<boolean> {
        return await this.perfectWeekBadge.isVisible().catch(() => false);
    }

    /**
     * Select a year from the dropdown
     */
    async selectYear(year: number): Promise<void> {
        await this.yearSelect.selectOption(year.toString());
        await this.waitForLoadingComplete();
    }

    /**
     * Check if welcome message shows correct user
     */
    async getWelcomeUserName(): Promise<string> {
        const text = await this.welcomeMessage.textContent() || '';
        // Extract name from "Welcome, {name}!"
        const match = text.match(/Welcome,\s*(.+)!/);
        return match ? match[1].trim() : '';
    }
}
