import { Page } from '@playwright/test';

/**
 * Test environment configuration for Playwright E2E tests
 *
 * LOCAL (SWA CLI):
 * - Proxying to the web app and Azure Functions
 * - Mock authentication at /.auth/login/google
 *
 * CLOUD (CI):
 * - Real Azure Static Web App
 * - Test auth bypass via X-Test-User-Email header (requires ENABLE_TEST_AUTH=true)
 *
 * Start services for local testing:
 *   ./start-local.sh   # Starts Azurite, Functions, Web App
 *   swa start http://localhost:5158 --api-devserver-url http://localhost:7071
 */

/**
 * Test environment constants and configuration
 */
export class TestEnvironment {
  // Admin email for mock authentication (must match ADMIN_EMAILS env var in Functions)
  public readonly adminEmail = 'test-admin@example.com';

  // SWA CLI URL - main entry point for tests (provides mock auth)
  public readonly swaUrl = 'http://localhost:4280';

  // Check if running in CI against cloud environment
  public readonly isCloudEnvironment: boolean;

  constructor() {
    // Detect cloud environment by checking BASE_URL env var
    const baseUrl = process.env.BASE_URL || '';
    this.isCloudEnvironment = baseUrl.includes('azurestaticapps.net');

    if (this.isCloudEnvironment) {
      console.log(`Running E2E tests against cloud environment: ${baseUrl}`);
      console.log('Using test auth bypass (X-Test-User-Email header)');
    } else {
      console.log('Running E2E tests locally with SWA CLI mock auth');
    }
  }

  /**
   * Authenticate using the appropriate method for the environment.
   * - Uses test auth bypass with X-Test-User-Email header (requires ENABLE_TEST_AUTH=true in Functions)
   * - This works for both local and cloud environments
   */
  async authenticate(adminPage: any, email?: string): Promise<void> {
    const authEmail = email || this.adminEmail;
    // Always use test auth bypass - it works for both local and cloud when ENABLE_TEST_AUTH=true
    console.log(`Setting up test auth bypass for ${authEmail}`);
    await adminPage.loginWithTestAuth(authEmail);
  }

  /**
   * Authenticate via sign-in button on any page (for non-admin auth flows).
   * Uses test auth bypass with X-Test-User-Email header (requires ENABLE_TEST_AUTH=true in Functions)
   * @param page - Playwright page object
   * @param email - Email to authenticate as (defaults to adminEmail)
   * @param returnUrl - Expected return URL pattern after auth (ignored, kept for compatibility)
   */
  async authenticateViaSignIn(page: Page, email?: string, returnUrl?: string): Promise<void> {
    const authEmail = email || this.adminEmail;
    console.log(`Setting up test auth bypass for ${authEmail}`);

    // Mock /.auth/me to make frontend think user is logged in
    await page.route('**/.auth/me', async route => {
      const mockAuthResponse = {
        clientPrincipal: {
          identityProvider: 'google',
          userId: `test-${authEmail.replace(/[^a-zA-Z0-9]/g, '')}`,
          userDetails: authEmail,
          userRoles: ['authenticated', 'anonymous'],
          claims: [
            { typ: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', val: authEmail },
            { typ: 'email', val: authEmail },
            { typ: 'name', val: authEmail.split('@')[0] }
          ]
        }
      };
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockAuthResponse)
      });
    });

    // Set up request interception to add test auth header
    await page.route('**/api/**', async route => {
      const headers = {
        ...route.request().headers(),
        'X-Test-User-Email': authEmail
      };
      await route.continue({ headers });
    });

    // Reload to apply mocked auth
    await page.reload();
    await page.waitForLoadState('networkidle');
    console.log('Test auth bypass configured');
  }

  /**
   * Set up test auth header for cloud environment only.
   * Call this early in a test to ensure API calls include the auth header.
   * Does nothing in local environment (where mock auth is used).
   */
  async setupTestAuthHeader(page: Page, email?: string): Promise<void> {
    if (!this.isCloudEnvironment) {
      return; // No-op in local environment
    }

    const authEmail = email || this.adminEmail;
    console.log(`Cloud auth: Setting up test auth bypass header for ${authEmail}`);

    await page.route('**/api/**', async route => {
      const headers = {
        ...route.request().headers(),
        'X-Test-User-Email': authEmail
      };
      await route.continue({ headers });
    });
  }
}
