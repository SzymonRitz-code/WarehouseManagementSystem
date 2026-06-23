import { RadioComponent } from './radio.component';

describe('RadioComponent', () => {
  it('emits selected value when enabled', () => {
    const component = new RadioComponent();
    const valueChange = vi.fn();
    component.value = 'PZ';
    component.disabled = false;
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).toHaveBeenCalledWith('PZ');
  });

  it('does not emit when disabled', () => {
    const component = new RadioComponent();
    const valueChange = vi.fn();
    component.value = 'WZ';
    component.disabled = true;
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).not.toHaveBeenCalled();
  });
});
