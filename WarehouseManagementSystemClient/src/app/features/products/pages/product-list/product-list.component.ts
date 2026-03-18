import { Component, OnInit } from '@angular/core';
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { TableComponent } from "../../../../shared/components/table/table.component";
import { Router } from '@angular/router';
import { ProductService } from '../../../services/product-service';
import { Product } from '../../model/product';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, TableComponent, ],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  constructor(private router: Router, private productService: ProductService) { }
  products: Product[] = [];
  ngOnInit(): void {
    this.products = this.productService.products;
  }


  columns = [
    { key: 'id', label: 'ID', sortable: true },
    { key: 'name', label: 'Name', sortable: true },
    { key: 'sku', label: 'SKU', sortable: true }
  ];
  goToForm() {
    this.router.navigate(['/products/form']);
  }
}
