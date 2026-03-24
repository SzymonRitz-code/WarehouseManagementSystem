import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ZoneService } from '../../../../services/zone-service';
import { ProductService } from '../../../../services/product-service';
import { CommonModule } from '@angular/common';
import { InputSelectComponent } from "../../../../../shared/components/form/input/input-select/input-select.component";
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../../shared/components/form/input/input-field.component";
import { ModalComponent } from "../../../../../shared/components/common/modal/modal.component";

@Component({
  selector: 'app-document-items-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputSelectComponent, LabelComponent, InputFieldComponent, ModalComponent],
  templateUrl: './document-items-form.component.html'
})
export class DocumentItemsFormComponent implements OnChanges, OnInit {
  @Input() isOpen = false;       // czy modal otwarty
  @Input() item: any = null;     // element do edycji
  @Output() save = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  itemForm!: FormGroup;

  productOptions: any[] = [];
  sourceZoneOptions: any[] = [];
  targetZoneOptions: any[] = [];

  constructor(private fb: FormBuilder,
    private zoneService: ZoneService,
    private productService: ProductService) {
    this.initOptions();
  }
  ngOnInit(): void {
    this.itemForm = this.fb.group({
      productId: [this.item?.productId ?? null, Validators.required],
      productName: ['', Validators.required],
      quantity: [this.item?.quantity ?? 1, [Validators.required, Validators.min(1)]],
      sourceZoneId: [this.item?.sourceZoneId ?? null],
      sourceZoneName: [''],
      targetZoneId: [this.item?.targetZoneId ?? null],
      targetZoneName: [''],
      id: [this.item?.id ?? null],
      index: [this.item?.index ?? null] // do rozpoznania edycji
    });
  }

  /** Inicjalizacja list produktów i stref */
  private initOptions() {
    this.productOptions = this.productService.products.map(p => ({ value: p.id, label: p.name }));
    this.sourceZoneOptions = this.zoneService.zones.map(z => ({ value: z.id, label: `${z.code}_${z.name}` }));
    this.targetZoneOptions = this.zoneService.zones.map(z => ({ value: z.id, label: `${z.code}_${z.name}` }));
  }

  /** Aktualizacja formularza przy zmianie wejściowego item */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      // otwarcie modala – inicjalizacja formularza
      this.itemForm = this.fb.group({
        productId: [this.item?.productId ?? null, Validators.required],
        productName: [''],
        quantity: [this.item?.quantity ?? 1, [Validators.required, Validators.min(1)]],
        sourceZoneId: [this.item?.sourceZoneId ?? null],
        sourceZoneName: [''],
        targetZoneId: [this.item?.targetZoneId ?? null],
        targetZoneName: [''],
        id: [this.item?.id ?? null],
        index: [this.item?.index ?? null] // do rozpoznania edycji
      });
    }
  }

  /** Zatwierdzenie modala */
  onSave() {
    if (this.itemForm.valid) {
      console.log(this.itemForm.value)
      this.save.emit(this.itemForm.value);
      this.resetForm();
      this.isOpen = false;
    }
  }

  /** Anulowanie modala */
  onCancel() {
    this.cancel.emit();
    this.resetForm();
  }

  /** Reset formularza */
  private resetForm() {
    this.itemForm.reset();
  }
}