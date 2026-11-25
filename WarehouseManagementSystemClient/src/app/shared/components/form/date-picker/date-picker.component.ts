import { Component, input, linkedSignal, output, ElementRef, ViewChild, Optional, Self } from '@angular/core';
import flatpickr from 'flatpickr';
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


  readonly id = input('');
  readonly mode = input<'single' | 'multiple' | 'range' | 'time'>('single');
  readonly defaultDate = input<string | Date | string[] | Date[]>();
  readonly required = input(false);
  readonly label = input<string>();
  readonly placeholder = input<string>();
  readonly disabled = input(false);
  readonly disabledState = linkedSignal(() => this.disabled());
  readonly dateChange = output<any>();

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
    let control = this.ngControl?.control;
    let base = `ui-control w-full appearance-none px-4 py-2.5`;
    if (control && control.invalid) base += ` border-red-500 focus:border-error-300 focus:ring-error-500/20`;
    return base;
  }

  ngAfterViewInit() {
    this.flatpickrInstance = flatpickr(this.dateInput.nativeElement, {
      mode: this.mode(),
      static: true,
      monthSelectorType: 'static',
      dateFormat: 'Y-m-d',
      defaultDate: this.defaultDate(),
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
    if (this.required() && !val) {
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
  setDisabledState(isDisabled: boolean): void {
    this.disabledState.set(isDisabled);
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
