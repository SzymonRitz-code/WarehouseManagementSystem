import { expect, test } from '../fixtures/wms.fixture';

test.describe('document workflow', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(!process.env.E2E_USERNAME || !process.env.E2E_PASSWORD, 'Authentication credentials are not configured.');
    await page.goto('/documents');
  });

  test('should create a draft document and expose it for further processing', async ({
    page,
    documentForm,
    pendingDocuments,
  }) => {
    await page.getByRole('button', { name: 'Add Document' }).click();
    const item = await documentForm.fillFirstItem({ type: 'PZ' });
    await documentForm.save();

    await expect(page).toHaveURL(/\/documents\/detail\/[0-9a-f-]+$/i);
    const details = page.locator('app-input-detail input');
    const documentNumber = (await details.nth(0).inputValue()).trim();
    await expect(details.nth(3)).toHaveValue('Draft');
    const itemRow = page.locator('app-document-items-detail tbody tr').first();
    await expect(itemRow.locator('input').nth(0)).toHaveValue(item.productName);
    await expect(itemRow.locator('input').nth(1)).toHaveValue(String(item.quantity));

    await pendingDocuments.open();
    await expect(pendingDocuments.rowContaining(documentNumber)).toContainText('Draft');
    await expect(pendingDocuments.rowContaining(documentNumber).getByRole('button', { name: 'Edit' })).toBeVisible();
  });

  test('should edit a draft and cancel it from pending work', async ({
    page,
    documentForm,
    pendingDocuments,
  }) => {
    await page.getByRole('button', { name: 'Add Document' }).click();
    await documentForm.fillFirstItem({ type: 'PZ' });
    await documentForm.save();
    await expect(page).toHaveURL(/\/documents\/detail\/[0-9a-f-]+$/i);

    const documentNumber = (await page.locator('app-input-detail input').nth(0).inputValue()).trim();
    await page.getByRole('button', { name: 'Edit' }).click();
    await expect(page).toHaveURL(/\/documents\/form\/[0-9a-f-]+$/i);
    await page.locator('textarea').first().fill('Edited by Playwright');
    await documentForm.save();
    await expect(page).toHaveURL(/\/documents\/detail\/[0-9a-f-]+$/i);
    await expect(page.locator('textarea').last()).toHaveValue('Edited by Playwright');

    await pendingDocuments.open();
    await pendingDocuments.cancel(documentNumber);
    await expect(pendingDocuments.rowContaining(documentNumber)).toHaveCount(0);

    await page.goto('/documents');
    await expect(page.getByRole('cell', { name: documentNumber })).toBeVisible();
    await expect(page.locator('tbody tr').filter({ hasText: documentNumber })).toContainText('Cancelled');
  });

  test('should confirm a valid pending document and remove it from pending work', async ({
    page,
    documentForm,
    pendingDocuments,
  }) => {
    await page.getByRole('button', { name: 'Add Document' }).click();
    await documentForm.fillFirstItem({ type: 'PZ' });
    await documentForm.save();
    await expect(page).toHaveURL(/\/documents\/detail\/[0-9a-f-]+$/i);
    const documentNumber = (await page.locator('app-input-detail input').nth(0).inputValue()).trim();

    await pendingDocuments.open();
    await pendingDocuments.confirm(documentNumber);
    await expect(pendingDocuments.rowContaining(documentNumber)).toHaveCount(0);

    await page.goto('/documents');
    const row = page.locator('tbody tr').filter({ hasText: documentNumber });
    await expect(row).toContainText('Confirmed');
    await expect(row.getByRole('button', { name: 'Details' })).toBeVisible();
  });

  test('should prevent confirmation when backend business validation fails', async ({
    page,
    documentForm,
    pendingDocuments,
  }) => {
    await page.getByRole('button', { name: 'Add Document' }).click();
    await documentForm.fillFirstItem({ type: 'WZ', quantity: 999_999_999 });
    await documentForm.save();
    await expect(page).toHaveURL(/\/documents\/detail\/[0-9a-f-]+$/i);
    const documentNumber = (await page.locator('app-input-detail input').nth(0).inputValue()).trim();

    await pendingDocuments.open();
    await pendingDocuments.rowContaining(documentNumber).getByRole('button', { name: 'Confirm' }).click();
    await page.getByRole('button', { name: 'Confirm document' }).click();

    await expect(page.getByText(/stock|available|insufficient|quantity/i).last()).toBeVisible();
    await expect(pendingDocuments.rowContaining(documentNumber)).toContainText('Draft');
    await expect(pendingDocuments.rowContaining(documentNumber).getByRole('button', { name: 'Confirm' })).toBeVisible();
  });
});
