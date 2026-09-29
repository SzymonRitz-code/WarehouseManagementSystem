import { expect, test } from '@playwright/test';

test('should report the failed OIDC discovery request when IdentityServer is unreachable', async ({ page }) => {
  await page.route('https://localhost:44380/**', (route) => route.abort('connectionrefused'));

  const discoveryFailure = page.waitForEvent('requestfailed', (request) => {
    const url = new URL(request.url());
    return url.origin === 'https://localhost:44380' &&
      url.pathname === '/.well-known/openid-configuration';
  });

  await page.goto('/documents');
  const failedRequest = await discoveryFailure;

  expect(failedRequest.failure()?.errorText).toMatch(/ERR_CONNECTION_REFUSED|ERR_ABORTED/);
});

test.describe('authenticated session', () => {
  test('should log out and protect the application again', async ({ page }) => {
    test.skip(!process.env.E2E_USERNAME || !process.env.E2E_PASSWORD, 'Authentication credentials are not configured.');

    await page.goto('/home');
    await page.getByRole('button', { name: /Musharof/ }).click();
    await page.getByRole('button', { name: 'Sign out' }).click();

    await expect(page).toHaveURL(/localhost:44380\/(Account\/Login|connect\/authorize)/);
  });
});
