import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-form-actions',
  imports: [],
  templateUrl: './form-actions.component.html'
})
export class FormActionsComponent {
  @Input() backLabel = 'Back';
  @Input() actionLabel = 'Save';
  @Input() disabled: boolean | null = true;
  @Output() back = new EventEmitter();
  @Output() submit = new EventEmitter();
}
