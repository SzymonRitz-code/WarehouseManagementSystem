import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';

import { InputSelectComponent } from './input-select.component';

describe('InputSelectComponent', () => {
  let component: InputSelectComponent;
  let fixture: ComponentFixture<InputSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InputSelectComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InputSelectComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('writes nullish values as an empty selection', () => {
    component.writeValue(null);

    expect(component.value).toBe('');
  });

  it('notifies Reactive Forms when selection changes', () => {
    const onChange = vi.fn();
    const onTouched = vi.fn();
    component.registerOnChange(onChange);
    component.registerOnTouched(onTouched);
    const event = { target: { value: 'wh-1' } } as unknown as Event;

    component.onValueChange(event);

    expect(component.value).toBe('wh-1');
    expect(onChange).toHaveBeenCalledWith('wh-1');
    expect(onTouched).toHaveBeenCalledTimes(1);
  });

  it('validates required selection', () => {
    component.required = true;

    expect(component.validate(new FormControl(''))).toEqual({ required: true });
    expect(component.errorMessages).toEqual(['This field is required.']);
    expect(component.validate(new FormControl('wh-1'))).toBeNull();
  });

  it('tracks disabled state from Reactive Forms', () => {
    component.setDisabledState(true);

    expect(component.disabled).toBe(true);
  });
});
