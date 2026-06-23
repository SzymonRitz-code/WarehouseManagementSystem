import { FormControl } from '@angular/forms';
import { DatePickerComponent } from './date-picker.component';

describe('DatePickerComponent', () => {
  it('validates required date values', () => {
    const component = new DatePickerComponent(null as any);
    component.required = true;

    expect(component.validate(new FormControl(''))).toEqual({ required: true });
    expect(component.errorMessages).toEqual(['This field is required.']);
    expect(component.validate(new FormControl('2026-06-22'))).toBeNull();
  });

  it('writes value to flatpickr instance when initialized', () => {
    const component = new DatePickerComponent(null as any);
    const setDate = vi.fn();
    (component as any).flatpickrInstance = { setDate };

    component.writeValue('2026-06-22');

    expect(setDate).toHaveBeenCalledWith('2026-06-22', true);
  });

  it('tracks disabled state from Reactive Forms', () => {
    const component = new DatePickerComponent(null as any);

    component.setDisabledState?.(true);

    expect(component.disabled).toBe(true);
  });

  it('destroys flatpickr instance on destroy', () => {
    const component = new DatePickerComponent(null as any);
    const destroy = vi.fn();
    (component as any).flatpickrInstance = { destroy };

    component.ngOnDestroy();

    expect(destroy).toHaveBeenCalledTimes(1);
  });
});
