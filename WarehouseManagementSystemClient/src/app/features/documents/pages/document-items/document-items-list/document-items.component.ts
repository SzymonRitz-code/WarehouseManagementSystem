import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ZoneService } from '../../../../zones/services/zone-service';
import { InputSelectComponent } from '../../../../../shared/components/form/input/input-select/input-select.component';
import { InputFieldComponent } from "../../../../../shared/components/form/input/input-field.component";
import { ProductService } from '../../../../products/services/product-service';
import { forkJoin, take } from 'rxjs';
import { ButtonComponent } from '../../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-document-items',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, InputSelectComponent, InputFieldComponent, ButtonComponent],
  templateUrl: './document-items.component.html',
})
export class DocumentItemsComponent implements OnInit {

  @Input() formArray: FormArray<FormGroup> = new FormArray<FormGroup>([]);


  productOptions: any[] = []
  sourceZoneOptions: any[] = [];
  targetZoneOptions: any[] = [];
  isModalOpened = false;
  modalItem: any = null; // pusty lub do edycji

  constructor(private fb: FormBuilder, private zoneService: ZoneService, private productService: ProductService) { }

  ngOnInit(): void {
    // RxJS insight: product and zone lookups are independent one-shot reads for select options.
    // forkJoin runs them in parallel and emits once when both complete. This is better than nested
    // subscribes because there is one deterministic initialization point and no duplicated zones
    // request for source/target selects. Do not use forkJoin for streams that should keep emitting.
    forkJoin({
      products: this.productService.getProducts(),
      zones: this.zoneService.getZones()
    }).pipe(take(1)).subscribe({
      next: ({ products, zones }) => {
        const zoneOptions = zones.map(z => ({ value: z.id, label: `${z.code}_${z.name}` }));

        this.productOptions = products.map(p => ({ value: p.id, label: p.name }));
        this.sourceZoneOptions = zoneOptions;
        this.targetZoneOptions = zoneOptions;
      }
    });
  }

  /** Otwiera modal do dodania nowej pozycji */
  openAddItemModal(): void {
    this.modalItem = null; // nowa pozycja
    this.isModalOpened = true;
  }

  /** Otwiera modal do edycji istniejącej pozycji */
  openEditItemModal(index: number): void {
    const item = this.formArray.at(index).value;
    this.modalItem = { ...item, index }; // przekaż index, żeby wiedzieć co aktualizować
    this.isModalOpened = true;
  }

  /** Callback z modala po zatwierdzeniu */
  onModalSave(item: any): void {
    if (item.index != null) {
      // edycja istniejącej pozycji
      this.formArray.at(item.index).patchValue(item);
    } else {
      // dodanie nowej pozycji
      this.formArray.push(this.fb.group(item));
    }
    this.isModalOpened = false;
  }

  /** Callback z modala po anulowaniu */
  onModalCancel(): void {
    this.isModalOpened = false;
  }

  /** Usunięcie pozycji */
  removeItem(index: number): void {
    this.formArray.removeAt(index);
  }

  /** Getter dla template */
  get items() {
    return this.formArray.controls;
  }
  addItemTolist(): void {
    const newItem = this.fb.group({
      id: [crypto.randomUUID()],
      productId: [null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      productBatchId: [null],
      sourceZoneId: [null, Validators.required],
      targetZoneId: [null],
    });

    this.formArray.push(newItem);
    this.formArray.get('documentItems')?.updateValueAndValidity();
  }
}
