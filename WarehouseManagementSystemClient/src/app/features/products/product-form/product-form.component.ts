import { Component } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { DefaultInputsComponent } from "../../../shared/components/form/form-elements/default-inputs/default-inputs.component";

@Component({
  selector: 'app-product-form',
  imports: [PageBreadcrumbComponent, DefaultInputsComponent],
  templateUrl: './product-form.component.html'
})
export class ProductFormComponent {

}
