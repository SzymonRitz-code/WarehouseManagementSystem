import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { SafeHtmlPipe } from '../../../pipe/safe-html.pipe';

@Component({
  selector: 'app-button',
  imports: [
    CommonModule,
    SafeHtmlPipe,
  ],
  templateUrl: './button.component.html',
  styles: ``,
  host: {

  },
})
export class ButtonComponent {

  @Input() size: 'sm' | 'md' = 'sm';
  @Input() variant: 'primary' | 'outline' = 'primary';
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() disabled = false;
  @Input() className = '';
  @Input() startIcon?: string; // SVG or icon class, or use ng-content for more flexibility
  @Input() endIcon?: string;

  @Output() btnClick = new EventEmitter<Event>();

  get sizeClasses(): string {
    return this.size === 'sm'
      ? 'px-3.5 py-2 text-sm'
      : 'px-4 py-2.5 text-sm';
  }

  get variantClasses(): string {
    return this.variant === 'primary'
      ? 'border border-brand-500 bg-brand-500 text-white shadow-theme-xs shadow-brand-500/20 hover:border-brand-600 hover:bg-brand-600 focus:ring-3 focus:ring-brand-500/20 disabled:border-brand-300 disabled:bg-brand-300 dark:border-brand-500/80 dark:bg-brand-500 dark:hover:border-brand-400 dark:hover:bg-brand-500/90'
      : 'border border-gray-300 bg-white text-gray-700 shadow-theme-xs hover:border-gray-400 hover:bg-gray-50 focus:ring-3 focus:ring-gray-500/10 dark:border-white/[0.08] dark:bg-white/[0.03] dark:text-gray-300 dark:hover:border-white/[0.14] dark:hover:bg-white/[0.06] dark:hover:text-white/90';
  }

  get disabledClasses(): string {
    return this.disabled ? 'cursor-not-allowed opacity-50' : '';
  }

  onClick(event: Event) {
    if (!this.disabled) {
      this.btnClick.emit(event);
    }
  }
}
