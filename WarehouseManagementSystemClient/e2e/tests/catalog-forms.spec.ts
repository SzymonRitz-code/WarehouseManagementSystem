import { expect, test } from '../fixtures/wms.fixture';
import { CatalogFormsPage } from '../pages/catalog-forms.page';

test.describe('catalog forms and aggregate items', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(!process.env.E2E_USERNAME || !process.env.E2E_PASSWORD, 'Authentication credentials are not configured.');
    await page.goto('/home');
  });

  test('should create a warehouse and show it in the warehouse table', async ({ page }) => {
    const forms = new CatalogFormsPage(page);
    const unique = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const warehouseCode = `E2E-${unique}`;
    const warehouseName = `E2E Warehouse ${unique}`;

    await forms.createWarehouse({ code: warehouseCode, name: warehouseName });
    await page.goto('/warehouses');

    const row = page.locator('tbody tr').filter({ hasText: warehouseCode });
    await expect(row).toBeVisible();
    await expect(row).toContainText(warehouseName);
    await expect(row).toContainText('Poland');
  });

  test('should add a zone to its warehouse and show the aggregate relationship in the zone table', async ({
    page,
  }) => {
    const forms = new CatalogFormsPage(page);
    const unique = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const warehouseCode = `E2E-${unique}`;
    const warehouseName = `E2E Warehouse ${unique}`;
    const zoneCode = `Z-${unique}`;
    const zoneName = `E2E Zone ${unique}`;

    await forms.createWarehouse({ code: warehouseCode, name: warehouseName });
    await forms.createZone({ code: zoneCode, name: zoneName, warehouseName });
    await page.goto('/zones');

    const row = page.locator('tbody tr').filter({ hasText: zoneCode });
    await expect(row).toBeVisible();
    await expect(row).toContainText(zoneName);
    await expect(row).toContainText(warehouseName);
    await expect(row).toContainText('Ambient');
  });

  test('should create a product and show its SKU in the product table', async ({ page }) => {
    const forms = new CatalogFormsPage(page);
    const unique = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const sku = `E2E-${unique}`;
    const name = `E2E Product ${unique}`;

    await forms.createProduct({ name, sku });
    await page.goto('/products');
    await page.getByPlaceholder('Search SKU or name').fill(sku);
    await page.getByRole('button', { name: 'Apply filters' }).click();

    const row = page.locator('tbody tr').filter({ hasText: sku });
    await expect(row).toBeVisible();
    await expect(row).toContainText(name);
    await expect(row.getByRole('button', { name: 'Batches' })).toBeVisible();
  });

  test('should add a batch to its product and show it in the product batch table', async ({ page }) => {
    const forms = new CatalogFormsPage(page);
    const unique = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
    const sku = `E2E-${unique}`;
    const productName = `E2E Product ${unique}`;
    const batchNumber = `BATCH-${unique}`;

    const productId = await forms.createProduct({ name: productName, sku });
    await forms.createBatch({ productId, batchNumber });
    await page.goto(`/products/${productId}/batches`);

    const row = page.locator('tbody tr').filter({ hasText: batchNumber });
    await expect(row).toBeVisible();
    await expect(row).toContainText(productName);
  });
});
