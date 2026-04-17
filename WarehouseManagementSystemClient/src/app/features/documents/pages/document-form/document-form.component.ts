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
import { WarehouseService } from '../../../warehouses/services/warehouse-service';
import { InputFieldComponent } from '../../../../shared/components/form/input/input-field.component';
import { DatePickerComponent } from "../../../../shared/components/form/date-picker/date-picker.component";
import { TextAreaComponent } from "../../../../shared/components/form/input/text-area.component";
import { DocumentItemsComponent } from "../document-items/document-items-list/document-items.component";
import { DocumentItem } from '../../model/document-item';

@Component({
  selector: 'app-document-form',
  standalone: true,
  imports: [
    PageBreadcrumbComponent,
    ComponentCardComponent,
    InputSelectComponent,
    InputFieldComponent,
    LabelComponent,
    FormActionsComponent,
    ReactiveFormsModule,
    CommonModule,
    DatePickerComponent,
    TextAreaComponent,
    DocumentItemsComponent
  ],
  templateUrl: './document-form.component.html'
})
export class DocumentFormComponent implements OnInit {
  id!: string;
  document!: Document | CreateDocument;
  documentForm!: FormGroup;
  sourceOptions!: any[];
  targetOptions!: any[];
  documentTyoeOptions!: any[];
  documentItemFormArray!: FormArray;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private documentService: DocumentService,
    private warehouseService: WarehouseService) { }


  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.documentForm = this.fb.nonNullable.group({
      number: ['', Validators.required],
      documentDate: [null, Validators.required],
      type: [null, Validators.required],
      notes: [''],
      sourceWarehouseId: [null],
      targetWarehouseId: [null],
      documentItems: this.fb.array([])
    })
    if (this.id) {
      this.document = this.documentService.getDocument(this.id);
      this.documentForm.patchValue({
        number: this.document.number,
        documentDate: this.document.documentDate,
        type: this.document.type,
        notes: this.document.notes,
        sourceWarehouseId: this.document.sourceWarehouseId,
        targetWarehouseId: this.document.targetWarehouseId,
      })
      this.documentItemFormArray = this.documentForm.get('documentItems') as FormArray;

      this.document.documentItems.forEach(item => {
        this.documentItemFormArray.push(this.fb.group({
          id: [item.id],
          productId: [item.productId, Validators.required],
          quantity: [item.quantity, [Validators.required, Validators.min(1)]],
          sourceZoneId: [item.sourceZoneId],
          targetZoneId: [item.targetZoneId],
        }));
      });
    }
    this.warehouseService.getWarehouses().subscribe({
      next: (responce) => {
        this.sourceOptions = responce.map(w => ({ value: w.id, label: w.name }));
      }
    })
    this.warehouseService.getWarehouses().subscribe({
      next: (responce) => {
        this.targetOptions = responce.map(w => ({ value: w.id, label: w.name }));
      }
    })
    this.documentTyoeOptions = Object.values(DocumentType).map(d => ({ value: d, label: d }))
  }
  get documentItemsFormArray(): FormArray {
    return this.documentForm.get('documentItems') as FormArray;
  }
  onSave() {
    this.document = this.documentForm.value;
    this.document = this.documentService.addDocument(this.document);
    this.router.navigateByUrl(`/documents/detail/${(this.document as Document).id}`);
  }
  onBack() {
    this.router.navigateByUrl('/documents');
  }

}
