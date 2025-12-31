import { Locator, Page } from '@playwright/test';

/**
 * Page Object Model for the Admin page
 * Handles authentication and file uploads through the admin UI
 */
export class AdminPage {
    readonly page: Page;

    // Authentication elements
    readonly loginButton: Locator;
    readonly logoutButton: Locator;
    readonly signedInAlert: Locator;
    readonly accessDeniedAlert: Locator;

    // Mock auth form elements (SWA CLI mock auth page)
    readonly mockAuthUsernameInput: Locator;
    readonly mockAuthSubmitButton: Locator;

    // Upload form elements (weekly lines)
    readonly weekInput: Locator;
    readonly yearInput: Locator;
    readonly fileInput: Locator;
    readonly uploadButton: Locator;
    readonly selectedFileAlert: Locator;

    // Bowl upload form elements
    readonly bowlYearInput: Locator;
    readonly bowlFileInput: Locator;
    readonly bowlUploadButton: Locator;
    readonly bowlSelectedFileAlert: Locator;

    // Status elements
    readonly successAlert: Locator;
    readonly errorAlert: Locator;
    readonly uploadSpinner: Locator;

    // Bowl status elements
    readonly bowlSuccessAlert: Locator;
    readonly bowlErrorAlert: Locator;
    readonly bowlUploadSpinner: Locator;

    constructor(page: Page) {
        this.page = page;

        // Authentication elements
        this.loginButton = page.getByRole('button', { name: /Sign in with Google/i });
        this.logoutButton = page.getByRole('button', { name: /Sign Out/i });
        this.signedInAlert = page.locator('.alert-info').filter({ hasText: 'Signed in as' });
        this.accessDeniedAlert = page.locator('.alert-warning').filter({ hasText: 'Access Denied' });

        // Mock auth form elements (SWA CLI provides these)
        // The SWA CLI mock auth page has a 'userDetails' field labeled as 'Username' - this is where email goes
        this.mockAuthUsernameInput = page.locator('input[name="userDetails"]');
        this.mockAuthSubmitButton = page.getByRole('button', { name: 'Login' });

        // Upload form elements (weekly lines)
        this.weekInput = page.locator('#weekInput');
        this.yearInput = page.locator('#yearInput');
        this.fileInput = page.locator('#fileInput');
        this.uploadButton = page.getByRole('button', { name: /Upload Lines/i });
        this.selectedFileAlert = page.locator('.alert-info').filter({ hasText: 'Selected file' }).first();

        // Bowl upload form elements
        this.bowlYearInput = page.locator('#bowlYearInput');
        this.bowlFileInput = page.locator('#bowlFileInput');
        this.bowlUploadButton = page.getByRole('button', { name: /Upload Bowl Lines/i });
        this.bowlSelectedFileAlert = page.locator('.alert-info').filter({ hasText: 'Selected file' }).last();

        // Status elements (for weekly lines section)
        this.successAlert = page.locator('.alert-success').first();
        this.errorAlert = page.locator('.alert-danger').first();
        this.uploadSpinner = page.locator('.spinner-border').first();

        // Bowl status elements
        this.bowlSuccessAlert = page.locator('.alert-success').filter({ hasText: /bowl/i });
        this.bowlErrorAlert = page.locator('.alert-danger').filter({ hasText: /bowl/i });
        this.bowlUploadSpinner = page.locator('.spinner-border').last();
    }

    /**
     * Navigate to the admin page
     */
    async goto(): Promise<void> {
        await this.page.goto('/admin');
        await this.page.waitForLoadState('networkidle');
    }

    /**
     * Check if user is authenticated
     */
    async isAuthenticated(): Promise<boolean> {
        // If we see the upload form (weekInput visible), we're authenticated
        const weekInputVisible = await this.weekInput.isVisible().catch(() => false);
        return weekInputVisible;
    }

    /**
     * Check if login button is visible (not authenticated)
     */
    async isLoginRequired(): Promise<boolean> {
        return await this.loginButton.isVisible().catch(() => false);
    }

    /**
     * Login using test auth bypass for cloud E2E testing.
     * Sets X-Test-User-Email header on all API requests.
     * Also mocks /.auth/me to make frontend think user is logged in.
     * Only works when ENABLE_TEST_AUTH=true in the cloud environment.
     * @param email - Email to use for test auth (should match ADMIN_EMAILS)
     */
    async loginWithTestAuth(email: string): Promise<void> {
        console.log(`Setting up test auth bypass for email: ${email}`);

        // Mock /.auth/me to make frontend think user is logged in
        await this.page.route('**/.auth/me', async route => {
            const mockAuthResponse = {
                clientPrincipal: {
                    identityProvider: 'google',
                    userId: `test-${email.replace(/[^a-zA-Z0-9]/g, '')}`,
                    userDetails: email,
                    userRoles: ['authenticated', 'anonymous'],
                    claims: [
                        { typ: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', val: email },
                        { typ: 'email', val: email },
                        { typ: 'name', val: email.split('@')[0] }
                    ]
                }
            };
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify(mockAuthResponse)
            });
        });

        // Intercept all API requests and add the test auth header
        await this.page.route('**/api/**', async route => {
            console.log(`[Route Handler] Intercepting: ${route.request().method()} ${route.request().url()}`);
            const headers = {
                ...route.request().headers(),
                'X-Test-User-Email': email
            };
            console.log(`[Route Handler] Adding X-Test-User-Email: ${email}`);
            await route.continue({ headers });
        });

        // Reload the page to ensure auth state is refreshed with mocked auth
        await this.page.reload();
        await this.page.waitForLoadState('networkidle');

        console.log('Test auth bypass configured');
    }

    /**
     * Login using SWA CLI mock authentication (local development only)
     * @param email - Email to use for mock auth (should match ADMIN_EMAILS)
     */
    async loginWithMockAuth(email: string): Promise<void> {
        // Click the login button which redirects to /.auth/login/google
        await this.loginButton.click();

        // Wait for the mock auth page to load
        await this.page.waitForURL('**/.auth/login/google**', { timeout: 10000 });

        // SWA CLI mock auth page has multiple fields
        // Fill in the userDetails field (labeled "Username") with the email
        const userDetailsInput = this.page.locator('input[name="userDetails"]');
        await userDetailsInput.waitFor({ state: 'visible', timeout: 10000 });
        await userDetailsInput.fill(email);

        // Also fill in claims with email claim - required by AuthHelper
        const claimsInput = this.page.locator('input[name="claims"]');
        if (await claimsInput.isVisible()) {
            const emailClaim = JSON.stringify([{ typ: 'email', val: email }]);
            await claimsInput.fill(emailClaim);
            await claimsInput.dispatchEvent('keyup');
        }

        // Trigger the keyup event to save to localStorage (SWA CLI uses this)
        await userDetailsInput.dispatchEvent('keyup');

        // Small delay to let localStorage save
        await this.page.waitForTimeout(100);

        // Submit the mock auth form
        await this.mockAuthSubmitButton.click();

        // Wait for redirect back to admin page
        await this.page.waitForURL('**/admin**', { timeout: 10000 });
        await this.page.waitForLoadState('networkidle');
    }

    /**
     * Upload a lines file through the admin UI
     * @param filePath - Absolute path to the Excel file
     * @param week - Week number
     * @param year - Year
     */
    async uploadLinesFile(filePath: string, week: number, year: number): Promise<void> {
        // Ensure we're on the admin page and authenticated
        if (await this.isLoginRequired()) {
            throw new Error('Not authenticated. Call loginWithMockAuth first.');
        }

        // Set up network logging to debug auth issues
        this.page.on('request', request => {
            if (request.url().includes('/api/')) {
                console.log(`API Request: ${request.method()} ${request.url()}`);
                console.log(`Headers: ${JSON.stringify(request.headers())}`);
            }
        });
        this.page.on('response', response => {
            if (response.url().includes('/api/')) {
                console.log(`API Response: ${response.status()} ${response.url()}`);
            }
        });

        // Fill in week number
        await this.weekInput.fill(week.toString());

        // Fill in year
        await this.yearInput.fill(year.toString());

        // Upload file using the file input
        await this.fileInput.setInputFiles(filePath);

        // Wait for file to be selected (UI shows selected file info)
        await this.selectedFileAlert.waitFor({ state: 'visible', timeout: 5000 });

        // Click upload button
        await this.uploadButton.click();

        // Wait for upload to complete (spinner disappears and success shows)
        await this.uploadSpinner.waitFor({ state: 'hidden', timeout: 30000 });

        // Check for success or error
        const hasSuccess = await this.successAlert.isVisible().catch(() => false);
        const hasError = await this.errorAlert.isVisible().catch(() => false);

        if (hasError) {
            const errorText = await this.errorAlert.textContent();
            throw new Error(`Upload failed: ${errorText}`);
        }

        if (!hasSuccess) {
            throw new Error('Upload did not show success message');
        }

        console.log(`Successfully uploaded lines for Week ${week}, Year ${year} via admin UI`);
    }

    /**
     * Get the success message text
     */
    async getSuccessMessage(): Promise<string> {
        if (await this.successAlert.isVisible()) {
            return await this.successAlert.textContent() || '';
        }
        return '';
    }

    /**
     * Get the error message text
     */
    async getErrorMessage(): Promise<string> {
        if (await this.errorAlert.isVisible()) {
            return await this.errorAlert.textContent() || '';
        }
        return '';
    }

    /**
     * Upload a bowl lines file through the admin UI
     * @param filePath - Absolute path to the Excel file
     * @param year - Year for bowl season
     */
    async uploadBowlLinesFile(filePath: string, year: number): Promise<void> {
        // Ensure we're on the admin page and authenticated
        if (await this.isLoginRequired()) {
            throw new Error('Not authenticated. Call loginWithMockAuth first.');
        }

        // Set up network logging to debug auth issues
        this.page.on('request', request => {
            if (request.url().includes('/api/')) {
                console.log(`API Request: ${request.method()} ${request.url()}`);
            }
        });
        this.page.on('response', response => {
            if (response.url().includes('/api/')) {
                console.log(`API Response: ${response.status()} ${response.url()}`);
            }
        });

        // Fill in year for bowl lines
        await this.bowlYearInput.fill(year.toString());

        // Upload file using the bowl file input
        await this.bowlFileInput.setInputFiles(filePath);

        // Wait for file to be selected - look for any alert-info with the file name
        await this.page.waitForTimeout(1000); // Give time for file selection

        // Click bowl upload button
        await this.bowlUploadButton.click();

        // Wait for upload to complete - wait for either success or error message
        const successLocator = this.page.locator('.alert-success');
        const errorLocator = this.page.locator('.alert-danger');
        
        // Wait for either success or error to appear
        await Promise.race([
            successLocator.waitFor({ state: 'visible', timeout: 30000 }).catch(() => {}),
            errorLocator.waitFor({ state: 'visible', timeout: 30000 }).catch(() => {})
        ]);

        // Check for success or error
        const hasSuccess = await successLocator.isVisible().catch(() => false);
        const hasError = await errorLocator.isVisible().catch(() => false);

        if (hasError) {
            const errorText = await errorLocator.textContent();
            throw new Error(`Bowl upload failed: ${errorText}`);
        }

        if (!hasSuccess) {
            throw new Error('Bowl upload did not show success message');
        }

        console.log(`Successfully uploaded bowl lines for Year ${year} via admin UI`);
    }

    /**
     * Get the bowl success message text
     */
    async getBowlSuccessMessage(): Promise<string> {
        const successLocator = this.page.locator('.alert-success').last();
        if (await successLocator.isVisible()) {
            return await successLocator.textContent() || '';
        }
        return '';
    }

    /**
     * Get the bowl error message text
     */
    async getBowlErrorMessage(): Promise<string> {
        const errorLocator = this.page.locator('.alert-danger').last();
        if (await errorLocator.isVisible()) {
            return await errorLocator.textContent() || '';
        }
        return '';
    }

    /**
     * Logout from admin
     */
    async logout(): Promise<void> {
        if (await this.logoutButton.isVisible()) {
            await this.logoutButton.click();
            await this.page.waitForLoadState('networkidle');
        }
    }
}
