import { Component, Input, Output, EventEmitter, ElementRef, ViewChild, forwardRef, Optional, Self } from '@angular/core';
import flatpickr from 'flatpickr';
import "flatpickr/dist/flatpickr.css";
import { AbstractControl, ControlValueAccessor, FormsModule, NG_VALIDATORS, NG_VALUE_ACCESSOR, NgControl, ReactiveFormsModule, ValidationErrors, Validator } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LabelComponent } from "../label/label.component";

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LabelComponent],
  templateUrl: './date-picker.component.html'
})
export class DatePickerComponent implements ControlValueAccessor, Validator {


  @Input() id!: string;
  @Input() mode: 'single' | 'multiple' | 'range' | 'time' = 'single';
  @Input() defaultDate?: string | Date | string[] | Date[];
  @Input() required: boolean = false;
  @Input() label?: string;
  @Input() placeholder?: string;
  @Input() disabled: boolean = false;
  @Output() dateChange = new EventEmitter<any>();

  constructor(@Optional() @Self() private ngControl: NgControl) {
    if (this.ngControl != null) {
      this.ngControl.valueAccessor = this;
    }
  }

  @ViewChild('dateInput', { static: false }) dateInput!: ElementRef<HTMLInputElement>;
  onChange: (value: any) => void = () => { };
  onTouched: () => void = () => { };

  private flatpickrInstance: flatpickr.Instance | undefined;
  errorMessages: string[] = [];

  get inputClasses(): string {
    let control = this.ngControl.control;
    let base = `h-11 w-full rounded-lg border appearance-none px-4 py-2.5 text-sm shadow-theme-xs placeholder:text-gray-400 focus:outline-hidden focus:ring-3  dark:bg-gray-900 dark:text-white/90 dark:placeholder:text-white/30  bg-transparent text-gray-800 border-gray-300 focus:border-brand-300 focus:ring-brand-500/20 dark:border-gray-700  dark:focus:border-brand-800`;
    if (control && control.invalid) base += ` border-red-500 focus:border-error-300 focus:ring-error-500/20`;
    return base;
  }

  ngAfterViewInit() {
    this.flatpickrInstance = flatpickr(this.dateInput.nativeElement, {
      mode: this.mode,
      static: true,
      monthSelectorType: 'static',
      dateFormat: 'Y-m-d',
      defaultDate: this.defaultDate,
      onChange: (selectedDates, dateStr, instance) => {
        this.onChange(dateStr);
        this.onTouched();
        this.updateErrors(dateStr);
        this.dateChange.emit({ selectedDates, dateStr, instance });
      }
    });
  }
  writeValue(value: any): void {
    if (this.flatpickrInstance) {
      this.flatpickrInstance.setDate(value, true);
    }
    if (this.dateInput) {
      this.dateInput.nativeElement.className = this.inputClasses;
    }
    this.updateErrors(value)
  }
  updateErrors(value: any): ValidationErrors | null {
    const errors: ValidationErrors = {};
    const val = value;
    this.errorMessages = [];
    if (this.required && !val) {
      errors['required'] = true;
      this.errorMessages.push('This field is required.');
    }
    return Object.keys(errors).length ? errors : null;
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
    return this.updateErrors(control.value);
  }
  ngOnDestroy() {
    if (this.flatpickrInstance) {
      this.flatpickrInstance.destroy();
    }
  }
}
