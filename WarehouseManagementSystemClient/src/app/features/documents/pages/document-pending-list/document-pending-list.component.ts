import { Component, OnInit } from '@angular/core';
import { DocumentService } from '../../services/document-service';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { Document } from '../../model/document';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { CommonModule } from '@angular/common';
import { DocumentType } from '../../../../core/enums/documentType';

@Component({
  selector: 'app-document-pending-list',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './document-pending-list.component.html'
})
export class DocumentPendingListComponent implements OnInit {
  goToForm() {
    this.router.navigateByUrl('/documents/form')
  }

  documents$!: Observable<Document[]>;
  columns = [
    { key: 'documentNumber', label: 'Document Number', sortable: true },        // numer nadany w systemie
    { key: 'type', label: 'Type', sortable: true },                             // typ: Receipt / Issue / Transfer / Adjustment
    { key: 'status', label: 'Status', sortable: true },                         // Draft / Confirmed / Completed / Cancelled
    { key: 'sourceWarehouse', label: 'From Warehouse', sortable: true },        // magazyn źródłowy (dla transferów/wydań)
    { key: 'destinationWarehouse', label: 'To Warehouse', sortable: true },     // magazyn docelowy (dla przyjęć/transferów)
    { key: 'createdBy', label: 'Created By', sortable: true },                  // kto utworzył dokument
    { key: 'approvedBy', label: 'Approved By', sortable: true },                // kto zatwierdził
    { key: 'createdAt', label: 'Created At', sortable: true, type: 'date' },    // data utworzenia
    { key: 'approvedAt', label: 'Approved At', sortable: true, type: 'date' },  // data zatwierdzenia
    { key: 'itemCount', label: 'Items', sortable: true },                       // liczba produktów w dokumencie
    { key: 'totalQuantity', label: 'Total Qty', sortable: true }                // suma ilości wszystkich produktów

  ];
  documentActions = [
    { label: 'Edit', action: 'edit', visible: (row: Document) => row.status === 'Draft' },
    { label: 'Confirm', action: 'confirm', visible: (row: Document) => row.status === 'Draft' },
    {
      label: 'Cancel', 
      action: 'cancel', 
      visible:
        (row: Document) => row.status === 'Draft' || (row.status === 'Confirmed' && row.type === DocumentType.MM)
    }
  ];

  constructor(private router: Router, private documentService: DocumentService) { }

  ngOnInit(): void {
    this.documents$ = this.documentService.getPendingDocuments();
  }

  onDocumentAction(event: { row: Document; action: string }) {
    const { row, action } = event;

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'confirm':
        this.onConfirm(row);
        break;

      case 'cancel':
        this.onCancel(row);
        break;
    }
  }
  onCancel(row: Document) {
    this.documentService.cancelDocument(row).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        this.documents$ = this.documentService.getDocuments(); // Odśwież listę dokumentów
      },
      error: (err) => {
        console.error('Error cancelling document:', err);
      }
    });
  }
  onConfirm(row: Document) {
    this.documentService.confirmDocument(row).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        this.documents$ = this.documentService.getPendingDocuments(); // Odśwież listę dokumentów
      },
      error: (err) => {
        console.error('Error confirming document:', err);
      }
    });
  }
  onDetails(row: Document) {
    this.router.navigateByUrl(`/documents/detail/${row.id}`)
  }
  onEdit(document: Document) {
    console.log(`Edit: ${document.id}`)
    this.router.navigateByUrl(`/documents/form/${document.id}`)
  }
}
