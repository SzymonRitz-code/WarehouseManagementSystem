import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { Document } from '../../model/document';
import { DocumentService } from '../../../services/document-service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, LabelComponent, InputDetailComponent, DetailActionsComponent],
  templateUrl: './document-detail.component.html'
})
export class DocumentDetailComponent implements OnInit {

  constructor(private documentService: DocumentService, private activatedRoute: ActivatedRoute, private router: Router) { }
  id!: string;
  document!: Document | undefined;

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.document = this.documentService.getDocument(this.id);
  }

  onEdit() {
    this.router.navigateByUrl(`/documents/form/${this.id}`)
  }
  onBack() {
    this.router.navigateByUrl('/documents')
  }

}
