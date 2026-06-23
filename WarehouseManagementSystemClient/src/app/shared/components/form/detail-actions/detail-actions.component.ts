import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonComponent } from '../../../ui/button/button.component';

@Component({
  selector: 'app-detail-actions',
  imports: [ButtonComponent],
  templateUrl: './detail-actions.component.html'
})
export class DetailActionsComponent {

  @Input() backLabel = 'Back';
  @Input() actionLabel = 'Edit';
  @Input() set saveLabel(value: string) {
    this.actionLabel = value;
  }
  @Input() disabled: boolean | null = false;
  @Output() back = new EventEmitter();
  @Output() action = new EventEmitter();
}
