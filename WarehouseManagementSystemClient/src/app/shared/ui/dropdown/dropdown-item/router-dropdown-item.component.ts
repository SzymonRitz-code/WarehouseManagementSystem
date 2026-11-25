import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-router-dropdown-item',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <a
      [routerLink]="to()"
      [ngClass]="combinedClasses()"
      (click)="handleClick()"
    >
      <ng-content></ng-content>
    </a>
  `,
})
export class RouterDropdownItemComponent {
  readonly to = input('');
  readonly baseClassName = input('ui-dropdown-item');
  readonly className = input('');
  readonly itemClick = output<void>();
  readonly click = output<void>();

  readonly combinedClasses = computed(() => `${this.baseClassName()} ${this.className()}`.trim());

  handleClick() {
    this.click.emit();
    this.itemClick.emit();
  }
}
