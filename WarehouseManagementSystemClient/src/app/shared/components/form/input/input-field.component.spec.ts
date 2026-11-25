import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { InputFieldComponent } from './input-field.component';

describe('InputFieldComponent', () => {
  let component: InputFieldComponent;
  let fixture: ComponentFixture<InputFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [InputFieldComponent] }).compileComponents();
    fixture = TestBed.createComponent(InputFieldComponent);
    component = fixture.componentInstance;
  });

  it('writes nullish values as an empty string', () => {
    component.writeValue(null);

    expect(component.value).toBe('');
  });

  it('notifies Reactive Forms and refreshes validation on value change', () => {
    const onChange = vi.fn();
    fixture.componentRef.setInput('required', true);
    fixture.componentRef.setInput('minlength', 3);
    component.registerOnChange(onChange);

    component.onValueChange('ab');

    expect(component.value).toBe('ab');
    expect(onChange).toHaveBeenCalledWith('ab');
    expect(component.errorMessages).toEqual(['Minimum length is 3 characters.']);
  });

  it('returns required, minlength and maxlength validation errors', () => {
    fixture.componentRef.setInput('required', true);
    fixture.componentRef.setInput('minlength', 2);
    fixture.componentRef.setInput('maxlength', 4);

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
    component.setDisabledState(true);

    expect(component.disabledState()).toBe(true);
  });
});
