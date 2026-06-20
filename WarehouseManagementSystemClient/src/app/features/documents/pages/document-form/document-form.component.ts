import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateDocument } from '../../model/create-document';
import { Document } from '../../model/document';
import { DocumentType } from '../../../../core/enums/documentType';
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentService } from '../../services/document-service';
import { CommonModule } from '@angular/common';
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { InputSelectComponent } from '../../../../shared/components/form/input/input-select/input-select.component';
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { DatePickerComponent } from "../../../../shared/components/form/date-picker/date-picker.component";
import { TextAreaComponent } from "../../../../shared/components/form/input/text-area.component";
import { DocumentItemsComponent } from "../document-items/document-items-list/document-items.component";
import { minFormArrayLength } from '../../../../core/guards/vaildators';
import { setServerErrors } from '../../../../core/helpsers/vaildation-helper.helper';
import { ValidationSummaryComponent } from '../../../../shared/components/form/validation-summary/validation-summary.component';
import { map, take } from 'rxjs';

@Component({
  selector: 'app-document-form',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    InputSelectComponent,
    LabelComponent,
    FormActionsComponent,
    ReactiveFormsModule,
    CommonModule,
    DatePickerComponent,
    TextAreaComponent,
    DocumentItemsComponent,
    ValidationSummaryComponent
  ],
  templateUrl: './document-form.component.html'
})
export class DocumentFormComponent implements OnInit {
  id!: string;
  document!: Document | CreateDocument;
  documentForm!: FormGroup;
  sourceOptions: any[] = [];
  targetOptions: any[] = [];
  documentTypeOptions: any[] = [];
  documentItemFormArray!: FormArray;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private documentService: DocumentService,
    private warehouseService: WarehouseService) { 
    }


  ngOnInit(): void {
    console.log('Initializing DocumentFormComponent...');
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    console.log('Document ID from route:', this.id);
    this.documentForm = this.fb.group({
      documentDate: [null, Validators.required],
      type: [null, Validators.required],
      notes: [null, [Validators.maxLength(1000)]],
      sourceWarehouseId: [null, Validators.required],
      targetWarehouseId: [null],
      items: this.fb.array([], [minFormArrayLength(1)])
    })
    if (this.id) {
      // RxJS insight: this is form initialization, so a finite subscribe is acceptable.
      // In the list/detail views the observable itself is the view state and the template owns
      // subscription via async pipe. Here we intentionally patch a Reactive Form once.
      // take(1) makes that one-shot contract explicit.
      this.documentService.getDocument(this.id).pipe(take(1)).subscribe
        ({
          next: (response) => {
            this.document = response;

            this.documentForm.patchValue({
              documentDate: this.document.documentDate,
              type: this.document.type,
              notes: this.document.notes,
              sourceWarehouseId: this.document.sourceWarehouseId,
              targetWarehouseId: this.document.targetWarehouseId,
            })
            this.documentItemFormArray = this.documentForm.get('items') as FormArray;
            (this.document as Document).items.forEach(item => {
              this.documentItemFormArray.push(this.fb.group({
                id: [item.id],
                productId: [item.productId, Validators.required],
                quantity: [item.quantity, [Validators.required, Validators.min(1)]],
                productBatchId: [item.productBatchId],
                sourceZoneId: [item.sourceZoneId, Validators.required],
                targetZoneId: [item.targetZoneId],
              }));
            });
          },
          error: (err) => {
            console.error('Error fetching document:', err);
          }
        });
    }
    // RxJS insight: source and target use the same lookup data. The previous style would often
    // call the same endpoint twice for two selects. Mapping once and assigning the same option
    // snapshot to both controls removes duplicate HTTP work and keeps both selects consistent.
    this.warehouseService.getWarehouses().pipe(
      take(1),
      map(warehouses => warehouses.map(w => ({ value: w.id, label: w.name })))
    ).subscribe({
      next: (options) => {
        this.sourceOptions = options;
        this.targetOptions = options;
      },
      error: (err) => {
        console.error('Error fetching warehouses:', err);
      }
    });
    this.documentTypeOptions = Object.values(DocumentType).map(d => ({ value: d, label: d }))
  }
  get documentItemsFormArray(): FormArray {
    return this.documentForm.get('items') as FormArray;
  }

  onSave() {
    const document = this.documentForm.getRawValue();
    const payload: CreateDocument = {
      ...document,
      items: document.items.map((item: any) => ({
        productId: item.productId,
        quantity: item.quantity,
        sourceZoneId: item.sourceZoneId,
        targetZoneId: item.targetZoneId,
        productBatchId: item.productBatchId
      }))
    };

    const responce$ = this.id
      ? this.documentService.updateDocument(this.id, payload)
      : this.documentService.addDocument(payload);

    // RxJS insight: save is a command with a side effect: navigate or show validation errors.
    // That is a good place for subscribe. The important part is that it stays finite and local,
    // instead of leaking a long-lived subscription from the component.
    responce$.pipe(take(1)).subscribe({
      next: (responce) => {
        const id = responce.id ?? this.id;
        this.router.navigateByUrl(`/documents/detail/${id}`);
      },
      error: (err) => {
        console.error(err);
        setServerErrors(err, this.documentForm);
      }
    })

  }
  onBack() {
    this.router.navigateByUrl('/documents');
  }

}
