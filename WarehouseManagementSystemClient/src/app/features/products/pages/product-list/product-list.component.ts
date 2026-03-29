import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { Product } from '../../model/product';
import { ProductService } from '../../services/product-service';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, PageBreadcrumbComponent, ComponentCardComponent, TableComponent,],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {

  products$!: Observable<Product[]>;
  columns = [
    { key: 'sku', label: 'SKU', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'unit', label: 'Unit', sortable: true },
    { key: 'requiresBatch', label: 'Requires Batch', sortable: true, type: "boolean" },
    { key: 'weight', label: 'Weight', sortable: true },
    { key: 'volume', label: 'Volume', sortable: true },
    { key: 'isActive', label: 'Is Active', sortable: true, type: "boolean" }
  ];
  productActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];

  constructor(private router: Router, private productService: ProductService) { }


  ngOnInit(): void {
    this.products$ = this.productService.getProducts();
  }

  goToForm() {
    this.router.navigate(['/products/form']);
  }
  onProductAction(event: { row: Product; action: string }) {
    const { row, action } = event;

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
        break;
    }
  }
  onDetails(row: Product) {
    this.router.navigateByUrl(`/products/detail/${row.id}`)
  }
  onEdit(row: Product) {
    this.router.navigateByUrl(`/products/form/${row.id}`)
  }
}
