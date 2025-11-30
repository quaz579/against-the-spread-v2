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

    // Upload form elements
    readonly weekInput: Locator;
    readonly yearInput: Locator;
    readonly fileInput: Locator;
    readonly uploadButton: Locator;
    readonly selectedFileAlert: Locator;

    // Status elements
    readonly successAlert: Locator;
    readonly errorAlert: Locator;
    readonly uploadSpinner: Locator;

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

        // Upload form elements
        this.weekInput = page.locator('#weekInput');
        this.yearInput = page.locator('#yearInput');
        this.fileInput = page.locator('#fileInput');
        this.uploadButton = page.getByRole('button', { name: /Upload Lines/i });
        this.selectedFileAlert = page.locator('.alert-info').filter({ hasText: 'Selected file' });

        // Status elements
        this.successAlert = page.locator('.alert-success');
        this.errorAlert = page.locator('.alert-danger');
        this.uploadSpinner = page.locator('.spinner-border');
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
     * Login using SWA CLI mock authentication
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
     * Logout from admin
     */
    async logout(): Promise<void> {
        if (await this.logoutButton.isVisible()) {
            await this.logoutButton.click();
            await this.page.waitForLoadState('networkidle');
        }
    }
}
