import { RadioSmComponent } from './radio-sm.component';

describe('RadioSmComponent', () => {
  it('emits selected value on change', () => {
    const component = new RadioSmComponent();
    const valueChange = vi.fn();
    component.value = 'active';
    component.valueChange.subscribe(valueChange);

    component.onChange();

    expect(valueChange).toHaveBeenCalledWith('active');
  });
});
