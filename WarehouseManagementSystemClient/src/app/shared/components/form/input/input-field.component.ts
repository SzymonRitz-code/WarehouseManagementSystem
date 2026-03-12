import { CommonModule } from '@angular/common';
import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS, Validator, AbstractControl, ValidationErrors, FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input-field',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputFieldComponent),
      multi: true,
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => InputFieldComponent),
      multi: true,
    },
  ],
  template: `
    <div class="relative">
      <input
        [type]="type"
        [id]="id"
        [name]="name"
        [placeholder]="placeholder"
        [ngClass]="inputClasses"
        [disabled]="disabled"
        [(ngModel)]="value"
        (ngModelChange)="onValueChange($event)"
        (blur)="onTouched()"
        [required]="required"
        [minlength]="minlength"
        [maxlength]="maxlength"
      />

      <p *ngIf="errorMessages.length" class="mt-1.5 text-xs text-error-500">
        <span *ngFor="let err of errorMessages">{{ err }}</span>
      </p>
    </div>
  `,
})
export class InputFieldComponent implements ControlValueAccessor, Validator {
  @Input() type: string = 'text';
  @Input() id?: string = '';
  @Input() name: string = '';
  @Input() placeholder?: string = '';
  @Input() disabled: boolean = false;
  @Input() className: string = '';

  @Input() required!: boolean;
  @Input() minlength!: number;
  @Input() maxlength!: number;

  value: string | number = '';

  errorMessages: string[] = [];

  onChange: (value: any) => void = () => {};
  onTouched: () => void = () => {};

  get inputClasses(): string {
    let base = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30 ${this.className}`;
    if (this.disabled) base += ` text-gray-500 border-gray-300 opacity-40 bg-gray-100 cursor-not-allowed`;
    if (this.errorMessages.length) base += ` border-error-500 focus:border-error-300 focus:ring-error-500/20`;
    return base;
  }

  // wywołanie przy każdej zmianie ngModel
  onValueChange(val: any) {
    this.value = val;
    this.onChange(val);
    this.updateErrors(val);
  }

  writeValue(value: any): void {
    this.value = value ?? '';
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

  private updateErrors(value: any): ValidationErrors | null {
    const errors: ValidationErrors = {};
    this.errorMessages = [];

    const val = value?.toString() ?? '';

    if (this.required && !val) {
      errors['required'] = true;
      this.errorMessages.push('This field is required.');
    }
    if (this.minlength && val.length < this.minlength) {
      errors['minlength'] = { requiredLength: this.minlength, actualLength: val.length };
      this.errorMessages.push(`Minimum length is ${this.minlength} characters.`);
    }
    if (this.maxlength && val.length > this.maxlength) {
      errors['maxlength'] = { requiredLength: this.maxlength, actualLength: val.length };
      this.errorMessages.push(`Maximum length is ${this.maxlength} characters.`);
    }

    return Object.keys(errors).length ? errors : null;
  }
}