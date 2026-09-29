import { test as base } from '@playwright/test';
import { DocumentFormPage } from '../pages/document-form.page';
import { PendingDocumentsPage } from '../pages/pending-documents.page';

type WmsFixtures = {
  documentForm: DocumentFormPage;
  pendingDocuments: PendingDocumentsPage;
};

export const test = base.extend<WmsFixtures>({
  documentForm: async ({ page }, use) => {
    await use(new DocumentFormPage(page));
  },
  pendingDocuments: async ({ page }, use) => {
    await use(new PendingDocumentsPage(page));
  },
});

export { expect } from '@playwright/test';
