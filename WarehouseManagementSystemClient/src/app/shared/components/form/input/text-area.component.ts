import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { AbstractControl, ControlValueAccessor, FormsModule, NG_VALIDATORS, NG_VALUE_ACCESSOR, ReactiveFormsModule, ValidationErrors, Validator } from '@angular/forms';


@Component({
  selector: 'app-text-area',
  standalone:true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TextAreaComponent),
      multi: true,
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => TextAreaComponent),
      multi: true,
    },
  ],
  template: `
    <div class="relative">
      <textarea
        [placeholder]="placeholder"
        [rows]="rows"
        [value]="value"
        (input)="onInput($event)"
        [disabled]="disabled"
        [ngClass]="textareaClasses"
      ></textarea>
      @if (hint) {
      <p
        class="mt-2 text-sm"
        [ngClass]="error ? 'text-error-500' : 'text-gray-500 dark:text-gray-400'">
        {{ hint }}
      </p>
      }
    </div>
  `,
  styles: ``
})
export class TextAreaComponent implements ControlValueAccessor, Validator {

  @Input() placeholder = 'Enter your message';
  @Input() rows = 3;
  @Input() value = '';
  @Input() className = '';
  @Input() disabled = false;
  @Input() error = false;
  @Input() hint = '';
  @Input() required = false;

  @Output() valueChange = new EventEmitter<string>();
  onChange: (value: any) => void = () => { };
  onTouched: () => void = () => { };
  private _onValidatorChange?: () => void;

  onInput(event: Event) {
    const val = (event.target as HTMLTextAreaElement).value;
    this.value = val;
    this.onChange(val);
    this.onTouched();
    this.valueChange.emit(val);
    this._onValidatorChange?.();
  }

  writeValue(obj: any): void {
    this.value = obj?.val ?? obj ?? '';
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  validate(control: AbstractControl): ValidationErrors | null {
    const errors: ValidationErrors = {};
    if (this.required && !control.value?.trim()) {
      errors['required'] = true;
      this.error = true;  // ustawia klasę do czerwonej obramówki
    } else {
      this.error = false;
    }
    return Object.keys(errors).length ? errors : null;
  }

  registerOnValidatorChange?(fn: () => void): void {
    this._onValidatorChange = fn;
  }

  get textareaClasses(): string {
    let base = `ui-control-sm w-full px-4 py-2.5 ${this.className} `;
    if (this.disabled) {
      base += 'cursor-not-allowed opacity-60';
    } else if (this.error) {
      base += 'focus:border-error-300 focus:ring-error-500/10 dark:focus:border-error-800';
    } else {
      base += 'dark:text-white/90';
    }
    return base;
  }
}
