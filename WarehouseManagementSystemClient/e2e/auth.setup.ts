import { expect, test as setup } from '@playwright/test';
import { authFile } from '../playwright.config';

const username = process.env.E2E_USERNAME;
const password = process.env.E2E_PASSWORD;

setup.skip(
  !username || !password,
  'Set E2E_USERNAME and E2E_PASSWORD to run authenticated WMS scenarios.',
);

setup('authenticate against the local Duende IdentityServer', async ({ page }) => {
  await page.goto('/documents');
  await expect(page).toHaveURL(/localhost:44380\/Account\/Login/);

  await page.getByLabel('Username').fill(username!);
  await page.getByLabel('Password').fill(password!);
  await page.getByRole('button', { name: 'Login' }).click();

  await expect(page).toHaveURL(/localhost:4200\/(home|documents)/);
  await expect(page.getByText('Document List').or(page.getByText('Dashboard'))).toBeVisible();
  await page.context().storageState({ path: authFile });
});
