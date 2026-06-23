import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ZoneService } from '../../../../zones/services/zone-service';
import { CommonModule } from '@angular/common';
import { InputSelectComponent } from "../../../../../shared/components/form/input/input-select/input-select.component";
import { LabelComponent } from "../../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../../shared/components/form/input/input-field.component";
import { ModalComponent } from "../../../../../shared/components/common/modal/modal.component";
import { ProductService } from '../../../../products/services/product-service';
import { forkJoin, take } from 'rxjs';
import { ButtonComponent } from '../../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-document-items-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputSelectComponent, LabelComponent, InputFieldComponent, ModalComponent, ButtonComponent],
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
    this.initOptions();
  }

  /** Inicjalizacja list produktów i stref */
  private initOptions() {
    // RxJS insight: constructors should stay side-effect light. Loading options in ngOnInit keeps
    // component creation cheap and gives one deterministic init point for dropdown data.
    // forkJoin is appropriate here because products and zones are independent HTTP calls and the
    // modal needs both lists before the selects are fully useful.
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
