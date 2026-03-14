import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-detail-actions',
  imports: [],
  templateUrl: './detail-actions.component.html'
})
export class DetailActionsComponent {

  @Input() backLabel = 'Back';
  @Input() actionLabel = 'Edit';
  @Input() disabled: boolean | null = true;
  @Output() back = new EventEmitter();
  @Output() action = new EventEmitter();
}
