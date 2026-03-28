import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ZoneService } from '../../../../services/zone-service';
import { InputSelectComponent } from '../../../../../shared/components/form/input/input-select/input-select.component';
import { InputFieldComponent } from "../../../../../shared/components/form/input/input-field.component";
import { Product } from '../../../../products/model/product';
import { ProductService } from '../../../../products/services/product-service';

@Component({
  selector: 'app-document-items',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, InputSelectComponent, InputFieldComponent],
  templateUrl: './document-items.component.html',
})
export class DocumentItemsComponent implements OnInit {

  @Input() formArray!: FormArray<FormGroup>;


  productOptions: any[] = []
  sourceZoneOptions: any[] = [];
  targetZoneOptions: any[] = [];
  isModalOpened = false;
  modalItem: any = null; // pusty lub do edycji

  constructor(private fb: FormBuilder, private zoneService: ZoneService, private productService: ProductService) { }

  ngOnInit(): void {
    // przygotowanie list stref
    this.productOptions = this.productService.products.map(p => ({ value: p.id, label: p.name }))
    this.sourceZoneOptions = this.zoneService.zones.map(z => ({ value: z.id, label: `${z.code}_${z.name}` }));
    this.targetZoneOptions = this.zoneService.zones.map(z => ({ value: z.id, label: `${z.code}_${z.name}` }));
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
      sourceZoneId: [null, Validators.required],
      targetZoneId: [null, Validators.required],
    });

    this.formArray.push(newItem);
  }
}