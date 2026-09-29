import { expect, test } from '@playwright/test';

test('should expose available stock with a business status', async ({ page }) => {
  test.skip(!process.env.E2E_USERNAME || !process.env.E2E_PASSWORD, 'Authentication credentials are not configured.');

  await page.goto('/stocks/availability');
  await expect(page.getByText('Stock Availability List')).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Available Quantity' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible();
  await expect(page.locator('tbody tr').first()).toBeVisible();
  await expect(page.locator('tbody')).toContainText(/In Stock|Low Stock|Out of Stock/);
});
