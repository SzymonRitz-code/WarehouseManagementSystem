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

@Component({
  selector: 'app-input-field',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
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
        [value]="value"
        (input)="onValueChange($event.target.value)"
        (blur)="onTouched()"
        [required]="required"
        [attr.minlength]="minlength"
        [attr.maxlength]="maxlength"
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
  @Input() required: boolean = false;
  @Input() minlength?: number;
  @Input() maxlength?: number;
  @Input() disabled: boolean = false;

  value: string | number = '';

  errorMessages: string[] = [];
  onChange: (value: any) => void = () => { };
  onTouched: () => void = () => { };

  get inputClasses(): string {
    let base = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3 dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30`;
    if (this.errorMessages.length) base += ` border-error-500 focus:border-error-300 focus:ring-error-500/20`;
    return base;
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

  /** Obsługa inputu */
  onValueChange(val: any) {
    this.value = val;
    this.onChange(val);
    this.updateErrors(val);
  }

  /** Walidacja */
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