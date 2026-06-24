import { Component, Input } from '@angular/core';
import { DocumentItem } from '../../../model/document-item';
import { InputDetailComponent } from '../../../../../shared/components/form/input/input-detail.component';

@Component({
  selector: 'app-document-items-detail',
  imports: [InputDetailComponent],
  templateUrl: './document-items-detail.component.html'
})
export class DocumentItemsDetailComponent {

  @Input() documentItems: DocumentItem[] = [];

}
