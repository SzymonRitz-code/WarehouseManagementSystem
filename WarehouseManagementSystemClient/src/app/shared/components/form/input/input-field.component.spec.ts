import { FormControl } from '@angular/forms';
import { InputFieldComponent } from './input-field.component';

describe('InputFieldComponent', () => {
  it('writes nullish values as an empty string', () => {
    const component = new InputFieldComponent();

    component.writeValue(null);

    expect(component.value).toBe('');
  });

  it('notifies Reactive Forms and refreshes validation on value change', () => {
    const component = new InputFieldComponent();
    const onChange = vi.fn();
    component.required = true;
    component.minlength = 3;
    component.registerOnChange(onChange);

    component.onValueChange('ab');

    expect(component.value).toBe('ab');
    expect(onChange).toHaveBeenCalledWith('ab');
    expect(component.errorMessages).toEqual(['Minimum length is 3 characters.']);
  });

  it('returns required, minlength and maxlength validation errors', () => {
    const component = new InputFieldComponent();
    component.required = true;
    component.minlength = 2;
    component.maxlength = 4;

    expect(component.validate(new FormControl(''))).toEqual({
      required: true,
      minlength: { requiredLength: 2, actualLength: 0 }
    });
    expect(component.validate(new FormControl('abcde'))).toEqual({
      maxlength: { requiredLength: 4, actualLength: 5 }
    });
    expect(component.validate(new FormControl('abc'))).toBeNull();
  });

  it('tracks disabled state from Reactive Forms', () => {
    const component = new InputFieldComponent();

    component.setDisabledState(true);

    expect(component.disabled).toBe(true);
  });
});
