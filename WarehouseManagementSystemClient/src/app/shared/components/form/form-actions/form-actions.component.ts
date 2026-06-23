import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonComponent } from '../../../ui/button/button.component';

@Component({
  selector: 'app-form-actions',
  imports: [ButtonComponent],
  templateUrl: './form-actions.component.html'
})
export class FormActionsComponent {
  @Input() backLabel = 'Back';
  @Input() actionLabel = 'Save';
  @Input() set saveLabel(value: string) {
    this.actionLabel = value;
  }
  @Input() disabled: boolean | null = true;
  @Output() back = new EventEmitter();
  @Output() submit = new EventEmitter();
}
