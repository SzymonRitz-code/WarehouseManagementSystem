import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { DatePickerComponent } from './date-picker.component';

describe('DatePickerComponent', () => {
  let component: DatePickerComponent;
  let fixture: ComponentFixture<DatePickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [DatePickerComponent] }).compileComponents();
    fixture = TestBed.createComponent(DatePickerComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('id', 'test-date');
  });

  it('validates required date values', () => {
    fixture.componentRef.setInput('required', true);

    expect(component.validate(new FormControl(''))).toEqual({ required: true });
    expect(component.errorMessages).toEqual(['This field is required.']);
    expect(component.validate(new FormControl('2026-06-22'))).toBeNull();
  });

  it('writes value to flatpickr instance when initialized', () => {
    const setDate = vi.fn();
    (component as any).flatpickrInstance = { setDate, destroy: vi.fn() };

    component.writeValue('2026-06-22');

    expect(setDate).toHaveBeenCalledWith('2026-06-22', true);
  });

  it('tracks disabled state from Reactive Forms', () => {
    component.setDisabledState(true);

    expect(component.disabledState()).toBe(true);
  });

  it('destroys flatpickr instance on destroy', () => {
    const destroy = vi.fn();
    (component as any).flatpickrInstance = { destroy };

    component.ngOnDestroy();

    expect(destroy).toHaveBeenCalledTimes(1);
  });
});
