import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CheckboxComponent } from './checkbox.component';

describe('CheckboxComponent', () => {
  let component: CheckboxComponent;
  let fixture: ComponentFixture<CheckboxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CheckboxComponent] }).compileComponents();
    fixture = TestBed.createComponent(CheckboxComponent);
    component = fixture.componentInstance;
  });

  it('coerces written values to booleans', () => {
    component.writeValue('truthy' as any);
    expect(component.value).toBe(true);

    component.writeValue(0 as any);
    expect(component.value).toBe(false);
  });

  it('notifies Reactive Forms when checked state changes', () => {
    const onChange = vi.fn();
    const onTouched = vi.fn();
    component.registerOnChange(onChange);
    component.registerOnTouched(onTouched);
    const event = { target: { checked: true } } as unknown as Event;

    component.onInputChange(event);

    expect(component.value).toBe(true);
    expect(onChange).toHaveBeenCalledWith(true);
    expect(onTouched).toHaveBeenCalledTimes(1);
  });

  it('tracks disabled state from Reactive Forms', () => {
    component.setDisabledState(true);

    expect(component.disabledState()).toBe(true);
  });
});
