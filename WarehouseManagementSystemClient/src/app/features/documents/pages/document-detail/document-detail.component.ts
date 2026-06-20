import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { DocumentItemsDetailComponent } from "../document-items/app-document-items-detail/app-document-items-detail.component";
import { ZoneService } from '../../../zones/services/zone-service';
import { ProductService } from '../../../products/services/product-service';
import { catchError, forkJoin, map, Observable, of, shareReplay, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [
    CommonModule,
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

  id!: string;
  document$!: Observable<Document | undefined>;

  constructor(
    private documentService: DocumentService,
    private warehouseService: WarehouseService,
    private productService: ProductService,
    private zoneService: ZoneService,
    private activatedRoute: ActivatedRoute,
    private router: Router) { }


  ngOnInit(): void {
    // RxJS insight: detail is a view-model stream, not a bag of independent subscriptions.
    // The previous imperative approach usually looks like: read route id, subscribe for document,
    // then subscribe again for products/zones/warehouses and mutate fields as each request returns.
    // This version keeps the dependency chain visible: route id -> document -> enriched document.
    // switchMap is used because a route change should cancel work for the previous id.
    this.document$ = this.activatedRoute.paramMap.pipe(
      map(params => params.get('id')!),
      tap(id => this.id = id),
      switchMap(id => this.documentService.getDocument(id)),
      switchMap(document => this.enrichDocument(document)),
      catchError(() => of(undefined)),
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  private enrichDocument(document: Document): Observable<Document> {
    // RxJS insight: forkJoin is best for independent one-shot reads where the UI needs one stable
    // result after all calls complete. If any lookup fails, its local catchError returns null, so a
    // missing product/zone name does not break the whole details page.
    //
    // Architectural note: this is a frontend composition compromise. For large documents the better
    // long-term solution is a backend detail DTO or batch lookup endpoint, because per-item product
    // and zone requests can become chatty.
    const sourceWarehouse$ = document.sourceWarehouseId
      ? this.warehouseService.getWarehouse(document.sourceWarehouseId).pipe(catchError(() => of(null)))
      : of(null);

    const targetWarehouse$ = document.targetWarehouseId
      ? this.warehouseService.getWarehouse(document.targetWarehouseId).pipe(catchError(() => of(null)))
      : of(null);

    const items$ = document.items.length > 0
      ? forkJoin(document.items.map(item =>
          forkJoin({
            product: this.productService.getProduct(item.productId).pipe(catchError(() => of(null))),
            sourceZone: item.sourceZoneId ? this.zoneService.getZone(item.sourceZoneId).pipe(catchError(() => of(null))) : of(null),
            targetZone: item.targetZoneId ? this.zoneService.getZone(item.targetZoneId).pipe(catchError(() => of(null))) : of(null)
          }).pipe(
            map(({ product, sourceZone, targetZone }) => ({
              ...item,
              productName: product?.name,
              sourceZoneName: sourceZone?.name,
              targetZoneName: targetZone?.name
            }))
          )
        ))
      : of([]);

    return forkJoin({
      sourceWarehouse: sourceWarehouse$,
      targetWarehouse: targetWarehouse$,
      items: items$
    }).pipe(
      map(({ sourceWarehouse, targetWarehouse, items }) => ({
        ...document,
        sourceWarehouseName: sourceWarehouse?.name,
        targetWarehouseName: targetWarehouse?.name,
        items
      }))
    );
  }

  onEdit() {
    this.router.navigateByUrl(`/documents/form/${this.id}`)
  }
  onBack() {
    this.router.navigateByUrl('/documents')
  }

}
