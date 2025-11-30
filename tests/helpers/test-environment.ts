/**
 * Test environment configuration for Playwright E2E tests
 * 
 * These tests expect services to be running via SWA CLI which provides:
 * - Proxying to the web app and Azure Functions
 * - Mock authentication at /.auth/login/google
 * 
 * Start services before running tests:
 *   ./start-local.sh   # Starts Azurite, Functions, Web App
 *   swa start http://localhost:5158 --api-devserver-url http://localhost:7071
 * 
 * Or use the combined command in package.json scripts.
 */

/**
 * Test environment constants and configuration
 */
export class TestEnvironment {
  // Admin email for mock authentication (must match ADMIN_EMAILS env var in Functions)
  public readonly adminEmail = 'test-admin@example.com';

  // SWA CLI URL - main entry point for tests (provides mock auth)
  public readonly swaUrl = 'http://localhost:4280';

  constructor() {
    // No initialization needed - services should already be running
  }
}
