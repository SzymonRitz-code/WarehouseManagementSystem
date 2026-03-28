import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { Document } from '../../model/document';
import { DocumentService } from '../../services/document-service';
import { ActivatedRoute, Router } from '@angular/router';
import { TextAreaComponent } from "../../../../shared/components/form/input/text-area.component";
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DocumentItemsComponent } from "../document-items/document-items-list/document-items.component";
import { DocumentItemsDetailComponent } from "../document-items/app-document-items-detail/app-document-items-detail.component";
import { ZoneService } from '../../../services/zone-service';
import { ProductService } from '../../../products/services/product-service';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    LabelComponent,
    InputDetailComponent,
    DetailActionsComponent,
    TextAreaComponent,
    DocumentItemsDetailComponent
  ],
  templateUrl: './document-detail.component.html'
})
export class DocumentDetailComponent implements OnInit {

  constructor(
    private documentService: DocumentService,
    private warhouseService: WarehouseService,
    private productService: ProductService,
    private zoneService: ZoneService,
    private activatedRoute: ActivatedRoute,
    private router: Router) { }
  id!: string;
  document!: Document | undefined;

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.document = this.documentService.getDocument(this.id);
    this.document.sourceWarehouseName = this.warhouseService.getWarehouse(this.document.sourceWarehouseId!)?.warehouseName;
    this.document.targetWarehouseName = this.warhouseService.getWarehouse(this.document.targetWarehouseId!)?.warehouseName;
    this.document.documentItems
    this.document.documentItems.forEach(item => {
      this.productService.getProduct(item.productId).subscribe({
        next:
          (res) => { item.productName = res.name; }
      });
      item.sourceZoneName = this.zoneService.getZone(item.sourceZoneId)?.name;
      item.targetZoneName = this.zoneService.getZone(item.targetZoneId)?.name;
    });

  }

  onEdit() {
    this.router.navigateByUrl(`/documents/form/${this.id}`)
  }
  onBack() {
    this.router.navigateByUrl('/documents')
  }

}
