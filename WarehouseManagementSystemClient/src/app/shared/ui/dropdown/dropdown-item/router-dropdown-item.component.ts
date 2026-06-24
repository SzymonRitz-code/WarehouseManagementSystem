import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-router-dropdown-item',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <a
      [routerLink]="to"
      [ngClass]="combinedClasses"
      (click)="handleClick()"
    >
      <ng-content></ng-content>
    </a>
  `,
})
export class RouterDropdownItemComponent {
  @Input() to!: string; // Required route path
  @Input() baseClassName = 'ui-dropdown-item';
  @Input() className = '';
  @Output() itemClick = new EventEmitter<void>();
  @Output() click = new EventEmitter<void>();

  get combinedClasses(): string {
    return `${this.baseClassName} ${this.className}`.trim();
  }

  handleClick() {
    this.click.emit();
    this.itemClick.emit();
  }
}
