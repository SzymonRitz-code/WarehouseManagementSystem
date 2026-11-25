import { CommonModule } from '@angular/common';
import { Component, forwardRef, input, linkedSignal } from '@angular/core';
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
        [type]="type()"
        [id]="id()"
        [name]="name()"
        [placeholder]="placeholder()"
        [ngClass]="inputClasses"
        [value]="value"
        [disabled]="disabledState()"
        (input)="onValueChange($event.target.value)"
        (blur)="onTouched()"
        [required]="required()"
        [attr.minlength]="minlength()"
        [attr.maxlength]="maxlength()"
      />

      <p *ngIf="errorMessages.length" class="mt-1.5 text-xs text-error-500">
        <span *ngFor="let err of errorMessages">{{ err }}</span>
      </p>
    </div>
  `,
})
export class InputFieldComponent implements ControlValueAccessor, Validator {
  readonly type = input('text');
  readonly id = input('');
  readonly name = input('');
  readonly placeholder = input('');
  readonly required = input(false);
  readonly minlength = input<number>();
  readonly maxlength = input<number>();
  readonly disabled = input(false);
  readonly disabledState = linkedSignal(() => this.disabled());

  value: string | number = '';

  errorMessages: string[] = [];
  onChange: (value: any) => void = () => { };
  onTouched: () => void = () => { };

  get inputClasses(): string {
    let base = `ui-control w-full appearance-none px-4 py-2.5`;
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
    this.disabledState.set(isDisabled);
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

    if (this.required() && !val) {
      errors['required'] = true;
      this.errorMessages.push('This field is required.');
    }
    const minlength = this.minlength();
    if (minlength && val.length < minlength) {
      errors['minlength'] = { requiredLength: minlength, actualLength: val.length };
      this.errorMessages.push(`Minimum length is ${minlength} characters.`);
    }
    const maxlength = this.maxlength();
    if (maxlength && val.length > maxlength) {
      errors['maxlength'] = { requiredLength: maxlength, actualLength: val.length };
      this.errorMessages.push(`Maximum length is ${maxlength} characters.`);
    }

    return Object.keys(errors).length ? errors : null;
  }
}
