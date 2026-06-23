import { SelectComponent } from './select.component';

describe('SelectComponent', () => {
  it('uses default value on init when value is empty', () => {
    const component = new SelectComponent();
    component.defaultValue = 'wh-1';

    component.ngOnInit();

    expect(component.value).toBe('wh-1');
  });

  it('keeps explicit value instead of overwriting it with default value', () => {
    const component = new SelectComponent();
    component.value = 'explicit';
    component.defaultValue = 'default';

    component.ngOnInit();

    expect(component.value).toBe('explicit');
  });

  it('emits selected value on change', () => {
    const component = new SelectComponent();
    const valueChange = vi.fn();
    component.valueChange.subscribe(valueChange);
    const event = { target: { value: 'zone-1' } } as unknown as Event;

    component.onChange(event);

    expect(component.value).toBe('zone-1');
    expect(valueChange).toHaveBeenCalledWith('zone-1');
  });
});
