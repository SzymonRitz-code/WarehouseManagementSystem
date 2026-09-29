import { expect, Page } from '@playwright/test';

export class PendingDocumentsPage {
  constructor(private readonly page: Page) {}

  async open(): Promise<void> {
    await this.page.goto('/documents/pending');
    await expect(this.page.getByText('Pending Documents List')).toBeVisible();
    await expect(this.page.getByText('Loading pending documents...')).toBeHidden();
  }

  rowContaining(documentNumber: string) {
    return this.page.locator('tbody tr').filter({ hasText: documentNumber });
  }

  async confirm(documentNumber: string): Promise<void> {
    const row = this.rowContaining(documentNumber);
    await expect(row).toBeVisible();
    await row.getByRole('button', { name: 'Confirm' }).click();
    await expect(this.page.getByRole('heading', { name: documentNumber })).toBeVisible();
    await this.page.getByRole('button', { name: 'Confirm document' }).click();
  }

  async cancel(documentNumber: string): Promise<void> {
    const row = this.rowContaining(documentNumber);
    await expect(row).toBeVisible();
    await row.getByRole('button', { name: 'Cancel' }).click();
    await expect(this.page.getByRole('heading', { name: documentNumber })).toBeVisible();
    await this.page.getByRole('button', { name: 'Cancel document' }).click();
  }

  async documentNumberFromFirstRow(): Promise<string> {
    const row = this.page.locator('tbody tr').filter({ has: this.page.getByRole('button', { name: 'Edit' }) }).first();
    await expect(row).toBeVisible();
    return (await row.locator('td').first().innerText()).trim();
  }
}
