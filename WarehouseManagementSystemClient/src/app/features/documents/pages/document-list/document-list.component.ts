import { Component, OnInit } from '@angular/core';
import { DocumentService } from '../../../services/document-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';

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

  goToForm() {
    this.router.navigateByUrl('/documents/form')
  }

}
