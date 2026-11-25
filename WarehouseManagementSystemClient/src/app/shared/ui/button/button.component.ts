import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
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

  readonly size = input<'sm' | 'md'>('sm');
  readonly variant = input<'primary' | 'outline'>('primary');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly className = input('');
  readonly startIcon = input<string>();
  readonly endIcon = input<string>();

  readonly btnClick = output<Event>();

  readonly sizeClasses = computed(() => this.size() === 'sm'
    ? 'px-3.5 py-2 text-sm'
    : 'px-4 py-2.5 text-sm');

  readonly variantClasses = computed(() => this.variant() === 'primary'
    ? 'border border-brand-500 bg-brand-500 text-white shadow-theme-xs shadow-brand-500/20 hover:border-brand-600 hover:bg-brand-600 focus:ring-3 focus:ring-brand-500/20 disabled:border-brand-300 disabled:bg-brand-300 dark:border-brand-500/80 dark:bg-brand-500 dark:hover:border-brand-400 dark:hover:bg-brand-500/90'
    : 'border border-gray-300 bg-white text-gray-700 shadow-theme-xs hover:border-gray-400 hover:bg-gray-50 focus:ring-3 focus:ring-gray-500/10 dark:border-white/[0.08] dark:bg-white/[0.03] dark:text-gray-300 dark:hover:border-white/[0.14] dark:hover:bg-white/[0.06] dark:hover:text-white/90');

  readonly disabledClasses = computed(() => this.disabled() ? 'cursor-not-allowed opacity-50' : '');

  onClick(event: Event) {
    if (!this.disabled()) {
      this.btnClick.emit(event);
    }
  }
}
