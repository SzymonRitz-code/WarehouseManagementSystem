import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-radio',
  imports: [
    CommonModule,
  ],
  template: `
  <label
  [attr.for]="id()"
  [ngClass]="
    'relative flex cursor-pointer select-none items-center gap-3 text-sm font-medium ' +
    (disabled()
      ? 'text-gray-300 dark:text-gray-600 cursor-not-allowed'
      : 'text-gray-700 dark:text-gray-400') +
    ' ' +
    className()
  "
>
  <input
    [id]="id()"
    [name]="name()"
    type="radio"
    [value]="value()"
    [checked]="checked()"
    (change)="onChange()"
    class="sr-only"
    [disabled]="disabled()"
  />
  <span
    [ngClass]="
      'flex h-5 w-5 items-center justify-center rounded-full border-[1.25px] ' +
      (checked()
        ? 'border-brand-500 bg-brand-500'
        : 'bg-transparent border-gray-300 dark:border-gray-700') +
      ' ' +
      (disabled()
        ? 'bg-gray-100 dark:bg-gray-700 border-gray-200 dark:border-gray-700'
        : '')
    "
  >
    <span
      [ngClass]="
        'h-2 w-2 rounded-full bg-white ' + (checked() ? 'block' : 'hidden')
      "
    ></span>
  </span>
  {{ label() }}
</label>
  `,
})
export class RadioComponent {

  readonly id = input('');
  readonly name = input('');
  readonly value = input('');
  readonly checked = input(false);
  readonly label = input('');
  readonly className = input('');
  readonly disabled = input(false);

  readonly valueChange = output<string>();

  onChange() {
    if (!this.disabled()) {
      this.valueChange.emit(this.value());
    }
  }
}
