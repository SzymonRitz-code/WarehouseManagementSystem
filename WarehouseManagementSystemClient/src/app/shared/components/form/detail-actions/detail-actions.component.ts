import { Component, computed, input, output } from '@angular/core';
import { ButtonComponent } from '../../../ui/button/button.component';

@Component({
  selector: 'app-detail-actions',
  imports: [ButtonComponent],
  templateUrl: './detail-actions.component.html'
})
export class DetailActionsComponent {
  readonly backLabel = input('Back');
  readonly actionLabel = input('Edit');
  readonly saveLabel = input<string>();
  readonly disabled = input<boolean | null>(false);
  readonly displayedActionLabel = computed(() => this.saveLabel() ?? this.actionLabel());

  readonly back = output<void>();
  readonly action = output<void>();
}
