import { SwitchComponent } from './switch.component';

describe('SwitchComponent', () => {
  it('starts from defaultChecked value', () => {
    const component = new SwitchComponent();
    component.defaultChecked = true;

    component.ngOnInit();

    expect(component.isChecked).toBe(true);
  });

  it('toggles and emits the new value when enabled', () => {
    const component = new SwitchComponent();
    const valueChange = vi.fn();
    component.valueChange.subscribe(valueChange);

    component.handleToggle();

    expect(component.isChecked).toBe(true);
    expect(valueChange).toHaveBeenCalledWith(true);
  });

  it('does not toggle when disabled', () => {
    const component = new SwitchComponent();
    const valueChange = vi.fn();
    component.disabled = true;
    component.valueChange.subscribe(valueChange);

    component.handleToggle();

    expect(component.isChecked).toBe(false);
    expect(valueChange).not.toHaveBeenCalled();
  });

  it('uses gray palette when configured', () => {
    const component = new SwitchComponent();
    component.color = 'gray';
    component.isChecked = true;

    expect(component.switchColors.background).toContain('bg-gray-800');
  });
});
