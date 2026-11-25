import { Component, computed, input, output } from '@angular/core';
import { ButtonComponent } from '../../../ui/button/button.component';

@Component({
  selector: 'app-form-actions',
  imports: [ButtonComponent],
  templateUrl: './form-actions.component.html'
})
export class FormActionsComponent {
  readonly backLabel = input('Back');
  readonly actionLabel = input('Save');
  readonly saveLabel = input<string>();
  readonly disabled = input<boolean | null>(true);
  readonly displayedActionLabel = computed(() => this.saveLabel() ?? this.actionLabel());

  readonly back = output<void>();
  readonly submit = output<void>();
}
