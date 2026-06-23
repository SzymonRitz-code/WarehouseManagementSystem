import { CheckboxComponent } from './checkbox.component';

describe('CheckboxComponent', () => {
  it('coerces written values to booleans', () => {
    const component = new CheckboxComponent();

    component.writeValue('truthy' as any);
    expect(component.value).toBe(true);

    component.writeValue(0 as any);
    expect(component.value).toBe(false);
  });

  it('notifies Reactive Forms when checked state changes', () => {
    const component = new CheckboxComponent();
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
    const component = new CheckboxComponent();

    component.setDisabledState(true);

    expect(component.disabled).toBe(true);
  });
});
