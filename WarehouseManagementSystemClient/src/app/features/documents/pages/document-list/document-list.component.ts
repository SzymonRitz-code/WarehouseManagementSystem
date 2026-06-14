import { Component, OnInit } from '@angular/core';
import { DocumentService } from '../../services/document-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentList } from '../../model/document';
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent],
  templateUrl: './document-list.component.html'
})
export class DocumentListComponent implements OnInit {

  documents$!: Observable<DocumentList[]>;

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

  documentActions = [    // np. podgląd, edycja, PDF, zatwierdzenie
    { label: 'Details', action: 'details' },
  ];

  constructor(private router: Router, private documentService: DocumentService) { }

  ngOnInit(): void {
    this.documents$ = this.documentService.getDocuments();
  }


  goToForm() {
    this.router.navigateByUrl('/documents/form')
  }
  onDocumentAction(event: { row: DocumentList; action: string }) {
    const { row, action } = event;
    console.log(`Action "${action}" triggered for document:`, row);
    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
        break;
    }
  }
  onCancel(row: DocumentList) {
    this.documentService.cancelDocument(row).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        console.log(`Document ${updatedDoc.id} cancelled.`);
        this.documents$ = this.documentService.getDocuments(); // Odśwież listę dokumentów
      },
      error: (err) => {
        console.error('Error cancelling document:', err);
      }
    });
  }
  onConfirm(row: DocumentList) {
    this.documentService.confirmDocument(row).subscribe({
      next: (updatedDoc) => {
        // Aktualizuj widok lub pokaż powiadomienie
        console.log(`Document ${updatedDoc.id} confirmed.`);
        this.documents$ = this.documentService.getDocuments(); // Odśwież listę dokumentów
      },
      error: (err) => {
        console.error('Error confirming document:', err);
      }
    });
  }
  onDetails(row: DocumentList) {
    this.router.navigateByUrl(`/documents/detail/${row.id}`)
  }
  onEdit(document: DocumentList) {
    console.log(`Edit: ${document.id}`)
    this.router.navigateByUrl(`/documents/form/${document.id}`)
  }
}
