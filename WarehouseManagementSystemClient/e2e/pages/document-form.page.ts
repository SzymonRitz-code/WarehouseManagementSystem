import { expect, Page } from '@playwright/test';

export class DocumentFormPage {
  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    await this.page.goto('/documents/form');
    await expect(this.page.getByText('Document', { exact: true }).first()).toBeVisible();
  }

  async fillFirstItem(options: { type: string; quantity?: number }): Promise<{ productName: string; quantity: number }> {
    const documentDate = this.page.locator('#inputDocumentDate');
    await documentDate.click();
    await this.page.locator('.flatpickr-day.today').click();
    await expect(documentDate).not.toHaveValue('');
    await this.page.locator('#inputType').selectOption(options.type);
    await this.page.locator('#inputSourceWarehouse').selectOption({ index: 1 });
    await this.page.getByRole('button', { name: '+ Add Item' }).click();

    const itemRow = this.page.locator('tbody tr').first();
    const product = itemRow.locator('select[id^="productid-"]');
    await product.selectOption({ index: 1 });
    const productName = (await product.locator('option:checked').innerText()).trim();
    const quantity = options.quantity ?? 1;
    await itemRow.locator('input[id^="quantity-"]').fill(String(quantity));
    await itemRow.locator('select[id^="sourceZoneId-"]').selectOption({ index: 1 });
    return { productName, quantity };
  }

  async save(): Promise<void> {
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
  }
}
