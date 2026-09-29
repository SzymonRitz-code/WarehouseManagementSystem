import { expect, Page } from '@playwright/test';

export class CatalogFormsPage {
  constructor(private readonly page: Page) {}

  async createWarehouse(data: { code: string; name: string }): Promise<void> {
    await this.page.goto('/warehouses');
    await this.page.getByRole('button', { name: 'Add Warehouse' }).click();
    await this.page.locator('#inputCode').fill(data.code);
    await this.page.locator('#inputWarehouseName').fill(data.name);
    await this.page.locator('#inputCountry').fill('Poland');
    await this.page.locator('#inputCity').fill('Warsaw');
    await this.page.locator('#inputAddres').fill('Test Street 1');
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(this.page).toHaveURL(/\/warehouses\/detail\/[0-9a-f-]+$/i);
  }

  async createZone(data: { code: string; name: string; warehouseName: string }): Promise<void> {
    await this.page.goto('/zones');
    await this.page.getByRole('button', { name: 'Add Zone' }).click();
    await this.page.locator('#inputZoneCode').fill(data.code);
    await this.page.locator('#inputName').fill(data.name);
    await this.page.locator('#inputTemperatureType').selectOption({ label: 'Ambient' });
    await this.page.locator('#inputWarehouse').selectOption({ label: data.warehouseName });
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(this.page).toHaveURL(/\/zones\/detail\/[0-9a-f-]+$/i);
  }

  async createProduct(data: { name: string; sku: string }): Promise<string> {
    await this.page.goto('/products');
    await this.page.getByRole('button', { name: 'Add Product' }).click();
    await expect(this.page.getByRole('button', { name: 'Save', exact: true })).toBeDisabled();
    await this.page.locator('#inputName').fill(data.name);
    await this.page.locator('#inputSKU').fill(data.sku);
    await this.page.locator('#inputUnit').selectOption('Piece');
    await this.page.locator('#weight').fill('1');
    await this.page.locator('#inputVolume').fill('1');
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(this.page).toHaveURL(/\/products\/detail\/[0-9a-f-]+$/i);

    const productId = this.page.url().match(/\/products\/detail\/([0-9a-f-]+)$/i)?.[1];
    if (!productId) {
      throw new Error('The product form did not navigate to the created product.');
    }

    return productId;
  }

  async createBatch(data: { productId: string; batchNumber: string }): Promise<void> {
    await this.page.goto(`/products/${data.productId}/batches`);
    await expect(this.page.getByRole('button', { name: 'Add Product Batch' })).toBeVisible();
    await this.page.getByRole('button', { name: 'Add Product Batch' }).click();
    await this.page.locator('#inputBatchNumber').fill(data.batchNumber);
    await this.page.locator('#inputProduct').selectOption(data.productId);
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(this.page).toHaveURL(
      new RegExp(`/products/${data.productId}/batches/detail/[0-9a-f-]+$`, 'i'),
    );
  }
}
