import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-form-actions',
  imports: [],
  templateUrl: './form-actions.component.html'
})
export class FormActionsComponent {
  @Input() backLabel = 'Wróć';
  @Input() saveLabel = 'Zapisz'
  @Input() disabled: boolean | null = true;
  @Output() back = new EventEmitter();
  @Output() save = new EventEmitter();
}
