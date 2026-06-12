import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { DocumentType } from '../../../../core/enums/documentType';
import { Document } from '../../model/document';
import { DocumentService } from '../../services/document-service';
import { ComponentCardComponent } from '../../../../shared/components/common/component-card/component-card.component';
import { ModalComponent } from '../../../../shared/components/common/modal/modal.component';
import { PageBreadcrumbComponent } from '../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { TableComponent } from '../../../../shared/components/table/table.component';

@Component({
  selector: 'app-document-pending-list',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent, ModalComponent],
  templateUrl: './document-pending-list.component.html'
})
export class DocumentPendingListComponent implements OnInit {
  documents$!: Observable<Document[]>;
  selectedDocument: Document | null = null;
  actionMode: 'confirm' | 'cancel' | null = null;
  isActionPending = false;
  actionError: string | null = null;

  columns = [
    { key: 'documentNumber', label: 'Document Number', sortable: true },
    { key: 'type', label: 'Type', sortable: true },
    { key: 'status', label: 'Status', sortable: true },
    { key: 'sourceWarehouse', label: 'From Warehouse', sortable: true },
    { key: 'destinationWarehouse', label: 'To Warehouse', sortable: true },
    { key: 'createdBy', label: 'Created By', sortable: true },
    { key: 'approvedBy', label: 'Approved By', sortable: true },
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' },
    { key: 'approvedAt', label: 'Approved At', sortable: true, type: 'date' },
    { key: 'itemCount', label: 'Items', sortable: true },
    { key: 'totalQuantity', label: 'Total Qty', sortable: true }
  ];

  documentActions = [
    { label: 'Edit', action: 'edit', visible: (row: Document) => row.status === 'Draft' },
    { label: 'Confirm', action: 'confirm', visible: (row: Document) => row.status === 'Draft' },
    {
      label: 'Cancel',
      action: 'cancel',
      visible: (row: Document) => row.status === 'Draft' || (row.status === 'Confirmed' && row.type === DocumentType.MM)
    }
  ];

  constructor(private router: Router, private documentService: DocumentService) {}

  ngOnInit(): void {
    this.documents$ = this.documentService.getPendingDocuments();
  }

  goToForm() {
    this.router.navigateByUrl('/documents/form');
  }

  onDocumentAction(event: { row: Document; action: string }) {
    const { row, action } = event;
    console.log(`Action "${action}" triggered for document:`, row);

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;
      case 'confirm':
        this.openActionModal(row, 'confirm');
        break;
      case 'cancel':
        this.openActionModal(row, 'cancel');
        break;
    }
  }

  openActionModal(row: Document, action: 'confirm' | 'cancel') {
    this.selectedDocument = row;
    this.actionMode = action;
    this.actionError = null;
  }

  closeActionModal() {
    if (this.isActionPending) return;
    this.selectedDocument = null;
    this.actionMode = null;
    this.actionError = null;
  }

  confirmAction() {
    if (!this.selectedDocument || !this.actionMode) return;
    console.log(this.selectedDocument);
    
    this.isActionPending = true;
    this.actionError = null;

    const request$ = this.actionMode === 'confirm'
      ? this.documentService.confirmDocument(this.selectedDocument)
      : this.documentService.cancelDocument(this.selectedDocument);

    request$.subscribe({
      next: () => {
        this.documents$ = this.documentService.getPendingDocuments();
        this.closeActionModal();
      },
      error: (err) => {
        this.actionError = this.resolveServerError(err);
      }
    }).add(() => {
      this.isActionPending = false;
    });
  }

  onDetails(row: Document) {
    this.router.navigateByUrl(`/documents/detail/${row.id}`);
  }

  onEdit(document: Document) {
    this.router.navigateByUrl(`/documents/form/${document.id}`);
  }

  private resolveServerError(err: any): string {
    return err?.error?.detail
      || err?.error?.title
      || err?.message
      || 'Operation failed. Please try again.';
  }
}
