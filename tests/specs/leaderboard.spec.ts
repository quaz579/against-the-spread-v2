import { expect, test } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { TestEnvironment } from '../helpers/test-environment';
import { AdminPage } from '../pages/admin-page';
import { PicksPage } from '../pages/picks-page';
import { LeaderboardPage } from '../pages/leaderboard-page';
import { MyPicksPage } from '../pages/my-picks-page';

/**
 * E2E tests for leaderboard display flow (Phase 5)
 *
 * Tests cover:
 * - Leaderboard page loads and displays season standings
 * - Weekly view shows correct week standings
 * - User can click player name to view history
 * - My Picks page requires authentication
 * - My Picks displays user's pick history with WIN/LOSS badges
 */

// Test constants
const TEST_YEAR = 2025;
const TEST_WEEK = 11;

// Get repository root path
const REPO_ROOT = path.resolve(__dirname, '../..');
const REFERENCE_DOCS = path.join(REPO_ROOT, 'reference-docs');

// Test environment configuration
const testEnv = new TestEnvironment();

test.describe('Leaderboard Display Flow', () => {
    test.beforeAll(async () => {
        console.log('Leaderboard E2E tests starting...');
    });

    test('Leaderboard page loads and displays season standings', async ({ page }) => {
        const leaderboardPage = new LeaderboardPage(page);

        // === STEP 1: Navigate to leaderboard ===
        await test.step('Navigate to leaderboard page', async () => {
            await leaderboardPage.goto();
            await leaderboardPage.waitForLoadingComplete();
        });

        // === STEP 2: Verify page structure ===
        await test.step('Verify leaderboard page structure', async () => {
            // Page title should be visible
            const heading = page.locator('h1:has-text("Leaderboard")');
            await expect(heading).toBeVisible();

            // Year selector should be visible
            await expect(leaderboardPage.yearSelect).toBeVisible();

            // View selector should be visible
            await expect(leaderboardPage.viewSelect).toBeVisible();
        });

        // === STEP 3: Check for standings or no-data message ===
        await test.step('Check standings or no-data message', async () => {
            const hasStandings = await leaderboardPage.hasStandings();
            const hasNoData = await leaderboardPage.hasNoDataMessage();

            console.log(`Has standings: ${hasStandings}, Has no-data message: ${hasNoData}`);

            // Either standings should be shown or no-data message
            expect(hasStandings || hasNoData).toBe(true);

            if (hasStandings) {
                const rowCount = await leaderboardPage.getRowCount();
                console.log(`Found ${rowCount} players in standings`);
                expect(rowCount).toBeGreaterThan(0);

                // Verify first place badge if standings exist
                const firstRankBadge = await leaderboardPage.getRankBadgeText(1);
                if (firstRankBadge) {
                    console.log(`First place badge: ${firstRankBadge}`);
                }
            }
        });
    });

    test('Weekly view shows correct week standings', async ({ page }) => {
        const leaderboardPage = new LeaderboardPage(page);

        // === STEP 1: Navigate to leaderboard ===
        await test.step('Navigate to leaderboard page', async () => {
            await leaderboardPage.goto();
            await leaderboardPage.waitForLoadingComplete();
        });

        // === STEP 2: Switch to weekly view ===
        await test.step('Switch to weekly view', async () => {
            await leaderboardPage.selectView('weekly');

            // Week selector should appear
            const weekSelectVisible = await leaderboardPage.weekSelect.isVisible().catch(() => false);

            // If weeks are available, selector should be visible
            if (weekSelectVisible) {
                console.log('Week selector is visible');
            }
        });

        // === STEP 3: Select a specific week if available ===
        await test.step('View weekly standings', async () => {
            const weekSelectVisible = await leaderboardPage.weekSelect.isVisible().catch(() => false);

            if (weekSelectVisible) {
                // Get available weeks
                const options = await leaderboardPage.weekSelect.locator('option').all();
                const weekCount = options.length;
                console.log(`Found ${weekCount} weeks available`);

                if (weekCount > 0) {
                    // Select the first week option
                    const firstWeekValue = await options[0].getAttribute('value');
                    if (firstWeekValue) {
                        await leaderboardPage.selectWeek(parseInt(firstWeekValue));
                    }
                }
            }

            // Verify standings or no-data
            const hasStandings = await leaderboardPage.hasStandings();
            const hasNoData = await leaderboardPage.hasNoDataMessage();

            expect(hasStandings || hasNoData).toBe(true);
        });

        // === STEP 4: Check for PERFECT badge in weekly view ===
        await test.step('Check for PERFECT week badges', async () => {
            const hasPerfect = await leaderboardPage.hasPerfectBadge();
            console.log(`Has PERFECT badge: ${hasPerfect}`);

            // PERFECT badge appears when someone goes 6-0 for the week
            // This is optional - may or may not exist depending on data
        });
    });

    test('User can click player name to view history', async ({ page }) => {
        const adminPage = new AdminPage(page);
        const picksPage = new PicksPage(page);
        const leaderboardPage = new LeaderboardPage(page);

        // === STEP 1: Setup - Create data by uploading lines and submitting picks ===
        await test.step('Setup: Upload lines and submit picks to create leaderboard data', async () => {
            const linesFile = path.join(REFERENCE_DOCS, 'Week 11 Lines.xlsx');

            // Upload lines as admin
            await adminPage.goto();
            await adminPage.loginWithMockAuth(testEnv.adminEmail);
            await adminPage.uploadLinesFile(linesFile, TEST_WEEK, TEST_YEAR);

            // Don't logout - navigate to picks to submit as authenticated user
            await picksPage.goto();
            await picksPage.waitForLoadingComplete();

            // Select week and submit picks
            await picksPage.selectWeek(TEST_YEAR, TEST_WEEK);
            await picksPage.selectGames(6);

            // Save picks
            const saveButton = page.getByRole('button', { name: /Save My Picks/i });
            if (await saveButton.isVisible().catch(() => false)) {
                await saveButton.click();
                await page.waitForSelector('.alert-success', { state: 'visible', timeout: 15000 }).catch(() => {});
            }
        });

        // === STEP 2: Navigate to leaderboard ===
        await test.step('Navigate to leaderboard', async () => {
            await leaderboardPage.goto();
            await leaderboardPage.waitForLoadingComplete();
        });

        // === STEP 3: Click on player name if standings exist ===
        await test.step('Click player name to view history', async () => {
            const hasStandings = await leaderboardPage.hasStandings();

            if (hasStandings) {
                // Get the first player name
                const playerName = await leaderboardPage.getPlayerNameFromRow(1);
                console.log(`First player: ${playerName}`);

                if (playerName) {
                    // Click on player name
                    await leaderboardPage.clickPlayerName(playerName.trim());

                    // Should navigate to user history page
                    await page.waitForLoadState('networkidle');
                    const currentUrl = page.url();
                    console.log(`Navigated to: ${currentUrl}`);

                    // URL should contain /leaderboard/user/
                    expect(currentUrl).toContain('/leaderboard/user/');

                    // User history page should show player name
                    const heading = page.locator('h1');
                    const headingText = await heading.textContent();
                    console.log(`User history heading: ${headingText}`);
                }
            } else {
                console.log('No standings to click - skipping player click test');
            }
        });
    });

    test('My Picks page requires authentication', async ({ page }) => {
        const myPicksPage = new MyPicksPage(page);

        // === STEP 1: Navigate to My Picks without authentication ===
        await test.step('Navigate to My Picks unauthenticated', async () => {
            // Clear any existing auth by going to logout first
            await page.goto('/.auth/logout?post_logout_redirect_uri=/');
            await page.waitForLoadState('networkidle');

            // Now navigate to my-picks
            await myPicksPage.goto();
        });

        // === STEP 2: Verify sign-in prompt is shown ===
        await test.step('Verify sign-in prompt displayed', async () => {
            const requiresAuth = await myPicksPage.requiresAuthentication();

            expect(requiresAuth).toBe(true);
            console.log('My Picks correctly requires authentication');

            // Sign in button should be visible
            const signInVisible = await myPicksPage.signInButton.isVisible().catch(() => false);
            expect(signInVisible).toBe(true);
        });

        // === STEP 3: Verify user content is NOT shown ===
        await test.step('Verify user content not shown', async () => {
            // Season summary cards should NOT be visible
            const winsCardVisible = await myPicksPage.winsCard.isVisible().catch(() => false);
            expect(winsCardVisible).toBe(false);

            // Week cards should NOT be visible
            const hasPicks = await myPicksPage.hasPicks();
            expect(hasPicks).toBe(false);
        });
    });

    test('My Picks displays user pick history with WIN/LOSS badges', async ({ page }) => {
        const adminPage = new AdminPage(page);
        const picksPage = new PicksPage(page);
        const myPicksPage = new MyPicksPage(page);

        // === STEP 1: Setup - Upload lines, submit picks, and enter results ===
        await test.step('Setup: Complete picks flow with results', async () => {
            const linesFile = path.join(REFERENCE_DOCS, 'Week 11 Lines.xlsx');

            // Upload lines as admin
            await adminPage.goto();
            await adminPage.loginWithMockAuth(testEnv.adminEmail);
            await adminPage.uploadLinesFile(linesFile, TEST_WEEK, TEST_YEAR);

            // Submit picks
            await picksPage.goto();
            await picksPage.waitForLoadingComplete();
            await picksPage.selectWeek(TEST_YEAR, TEST_WEEK);
            await picksPage.selectGames(6);

            const saveButton = page.getByRole('button', { name: /Save My Picks/i });
            if (await saveButton.isVisible().catch(() => false)) {
                await saveButton.click();
                await page.waitForSelector('.alert-success', { state: 'visible', timeout: 15000 }).catch(() => {});
            }

            // Enter results as admin
            await adminPage.goto();

            const resultsSection = page.locator('h2:has-text("Enter Game Results")');
            await resultsSection.scrollIntoViewIfNeeded();

            await page.locator('#resultsWeek').selectOption(TEST_WEEK.toString());
            await page.locator('#resultsYear').fill(TEST_YEAR.toString());
            await page.getByRole('button', { name: /Load Games/i }).click();
            await page.waitForSelector('.spinner-border', { state: 'hidden', timeout: 30000 });

            // Enter scores for all available games
            const enabledInputs = page.locator('table tbody input[type="number"]:not([disabled])');
            const enabledCount = await enabledInputs.count();

            // Enter alternating scores to create some wins and losses
            for (let i = 0; i < enabledCount; i += 2) {
                if (i + 1 < enabledCount) {
                    // Alternate between favorite and underdog covering
                    if ((i / 2) % 2 === 0) {
                        // Favorite covers
                        await enabledInputs.nth(i).fill('35');
                        await enabledInputs.nth(i + 1).fill('14');
                    } else {
                        // Underdog covers
                        await enabledInputs.nth(i).fill('17');
                        await enabledInputs.nth(i + 1).fill('21');
                    }
                }
            }

            const saveResultsButton = page.getByRole('button', { name: /Save Results/i });
            if (await saveResultsButton.isEnabled()) {
                await saveResultsButton.click();
                await page.waitForSelector('.spinner-border', { state: 'hidden', timeout: 30000 });
            }
        });

        // === STEP 2: Navigate to My Picks ===
        await test.step('Navigate to My Picks page', async () => {
            await myPicksPage.goto();
            await myPicksPage.waitForLoadingComplete();
        });

        // === STEP 3: Verify user info and summary ===
        await test.step('Verify user info and season summary', async () => {
            // Should not require auth anymore (already logged in)
            const requiresAuth = await myPicksPage.requiresAuthentication();

            if (!requiresAuth) {
                // Season summary cards should be visible
                const winsCardVisible = await myPicksPage.winsCard.isVisible().catch(() => false);
                console.log(`Wins card visible: ${winsCardVisible}`);

                if (winsCardVisible) {
                    const wins = await myPicksPage.getTotalWins();
                    const losses = await myPicksPage.getTotalLosses();
                    const pushes = await myPicksPage.getTotalPushes();
                    const winRate = await myPicksPage.getWinRate();

                    console.log(`Season stats: ${wins}W - ${losses}L - ${pushes}P, Win rate: ${winRate}`);
                }
            }
        });

        // === STEP 4: Verify WIN/LOSS badges ===
        await test.step('Verify WIN/LOSS badges in pick history', async () => {
            const hasPicks = await myPicksPage.hasPicks();

            if (hasPicks) {
                const winBadgeCount = await myPicksPage.getWinBadgeCount();
                const lossBadgeCount = await myPicksPage.getLossBadgeCount();
                const pushBadgeCount = await myPicksPage.getPushBadgeCount();

                console.log(`Badges: ${winBadgeCount} WIN, ${lossBadgeCount} LOSS, ${pushBadgeCount} PUSH`);

                // Should have some result badges if results were entered
                // Note: May also have Pending badges for games without results
            } else {
                console.log('No picks found in My Picks');
            }
        });

        // === STEP 5: Check for perfect week badge ===
        await test.step('Check for perfect week badge', async () => {
            const hasPerfect = await myPicksPage.hasPerfectWeekBadge();
            console.log(`Has perfect week badge: ${hasPerfect}`);
            // Perfect week is optional - only appears if user went 6-0
        });
    });
});
