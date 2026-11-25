import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dropdown-item',
  templateUrl: './dropdown-item.component.html',
  imports: [CommonModule, RouterModule]
})
export class DropdownItemComponent {
  readonly to = input<string>();
  readonly baseClassName = input('ui-dropdown-item');
  readonly className = input('');
  readonly itemClick = output<void>();
  readonly click = output<void>();

  readonly combinedClasses = computed(() => `${this.baseClassName()} ${this.className()}`.trim());

  handleClick(event: Event) {
    event.preventDefault();
    this.click.emit();
    this.itemClick.emit();
  }
}
