import { Component, OnInit } from '@angular/core';
import { DocumentService } from '../../../services/document-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { Document } from '../../model/document';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './document-list.component.html'
})
export class DocumentListComponent implements OnInit {

  documents: any[] = [];
  constructor(private documentService: DocumentService, private router: Router) { }
  ngOnInit(): void {
    this.documents = this.documentService.documentList;
  }

  columns = [
    { key: 'id', label: 'Document ID', sortable: true },                        // unikalny numer dokumentu
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
    { key: 'totalQuantity', label: 'Total Qty', sortable: true },               // suma ilości wszystkich produktów
    { key: 'actions', label: ' ', sortable: false }                             // np. podgląd, edycja, PDF, zatwierdzenie
  ];
  documentActions = [
    { label: 'Edit', action: 'edit', visible: (row: Document) => row.status === 'Draft' },
    { label: 'Details', action: 'details' },
    { label: 'Confirm', action: 'confirm', visible: (row: Document) => row.status === 'Draft' },
    { label: 'Cancel', action: 'cancel', visible: (row: Document) => row.status !== 'Cancelled' },
  ];
  goToForm() {
    this.router.navigateByUrl('/documents/form')
  }
  onDocumentAction(event: { row: Document; action: string }) {
    const { row, action } = event;

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
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
    throw new Error('Method not implemented.');
  }
  onConfirm(row: Document) {
    throw new Error('Method not implemented.');
  }
  onDetails(row: Document) {
    throw new Error('Method not implemented.');
  }
  onEdit(document: Document) {
    console.log(`Edit: ${document.id}`)
    this.router.navigateByUrl(`/documents/form/${document.id}`)
  }
}
