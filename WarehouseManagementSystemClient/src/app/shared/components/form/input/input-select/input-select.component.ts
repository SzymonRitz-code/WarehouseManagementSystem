import { CommonModule } from '@angular/common';
import { Component, Input, forwardRef } from '@angular/core';
import {
  ControlValueAccessor,
  NG_VALUE_ACCESSOR,
  NG_VALIDATORS,
  Validator,
  AbstractControl,
  ValidationErrors,
  FormsModule,
  ReactiveFormsModule
} from '@angular/forms';

export interface Option {
  value: string;
  label: string;
}

@Component({
  selector: 'app-input-select',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
  <div class="relative">
  <select
    [id]="id"
    [name]="name"
    [ngClass]="selectClasses"
    [value]="value"
    (change)="onValueChange($event)"
    [disabled]="disabled"
  >
    <!-- Placeholder -->
    <option value="" disabled [selected]="!value"
     class="text-gray-700 dark:bg-gray-900 dark:text-gray-400"
    >{{ placeholder }}</option>

    <!-- Options -->
    <option *ngFor="let option of options" [value]="option.value"
    class="text-gray-700 dark:bg-gray-900 dark:text-gray-400"
    >
      {{ option.label }}
    </option>
  </select>

  <!-- Validation errors -->
  <p *ngIf="errorMessages.length" class="mt-1.5 text-xs text-error-500">
    <span *ngFor="let err of errorMessages">{{ err }}</span>
  </p>
</div>`,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputSelectComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => InputSelectComponent),
      multi: true
    }
  ]
})
export class InputSelectComponent implements ControlValueAccessor, Validator {
  @Input() id?: string = '';
  @Input() name?: string = '';
  @Input() options: Option[] = [];
  @Input() placeholder: string = 'Select an option';
  @Input() className: string = '';
  @Input() required: boolean = false;
  @Input() disabled: boolean = false;

  value: string = '';
  errorMessages: string[] = [];

  onChange: (value: any) => void = () => { };
  onTouched: () => void = () => { };

  get selectClasses(): string {
    let base = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30`;
    if (this.errorMessages.length) {
      base += ` border-error-500 focus:border-error-300 focus:ring-error-500/20`;
    }
    return base + ` ${this.className}`;
  }

  /** Reactive Forms */
  writeValue(val: any): void {
    this.value = val ?? '';
    this.updateErrors(this.value);
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  validate(control: AbstractControl): ValidationErrors | null {
    return this.updateErrors(control.value);
  }

  onValueChange(event: Event) {
    const target = event.target as HTMLSelectElement | null; // rzutowanie + null check
    if (!target) return;

    const val = target.value;
    this.value = val;
    this.onChange(val);      // powiadamiamy ReactiveForms
    this.onTouched();
    this.updateErrors(val);  // aktualizacja walidacji
  }

  private updateErrors(value: any): ValidationErrors | null {
    const errors: ValidationErrors = {};
    this.errorMessages = [];

    const val = value?.toString() ?? '';

    if (this.required && !val) {
      errors['required'] = true;
      this.errorMessages.push('This field is required.');
    }

    return Object.keys(errors).length ? errors : null;
  }
}