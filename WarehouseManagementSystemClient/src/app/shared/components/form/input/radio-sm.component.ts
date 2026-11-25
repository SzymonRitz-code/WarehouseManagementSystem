import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-radio-sm',
  imports: [CommonModule],
  template: `
    <label
      [attr.for]="id()"
      [ngClass]="'flex cursor-pointer select-none items-center text-sm text-gray-500 dark:text-gray-400 ' + className()"
    >
      <span class="relative">
        <!-- Hidden Input -->
        <input
          type="radio"
          [id]="id()"
          [name]="name()"
          [value]="value()"
          [checked]="checked()"
          (change)="onChange()"
          class="sr-only"
        />
        <!-- Styled Radio Circle -->
        <span
          [ngClass]="
            'mr-2 flex h-4 w-4 items-center justify-center rounded-full border ' +
            (checked()
              ? 'border-brand-500 bg-brand-500'
              : 'bg-transparent border-gray-300 dark:border-gray-700')
          "
        >
          <!-- Inner Dot -->
          <span
            [ngClass]="
              'h-1.5 w-1.5 rounded-full ' +
              (checked() ? 'bg-white' : 'bg-white dark:bg-[#1e2636]')
            "
          ></span>
        </span>
      </span>
      {{ label() }}
    </label>
  `,
  styles: ``
})
export class RadioSmComponent {

  readonly id = input('');
  readonly name = input('');
  readonly value = input('');
  readonly checked = input(false);
  readonly label = input('');
  readonly className = input('');

  readonly valueChange = output<string>();

  onChange() {
    this.valueChange.emit(this.value());
  }
}
