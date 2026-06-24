import { RouterDropdownItemComponent } from './router-dropdown-item.component';

describe('RouterDropdownItemComponent', () => {
  it('combines base and custom classes', () => {
    const component = new RouterDropdownItemComponent();
    component.baseClassName = 'base';
    component.className = 'custom';

    expect(component.combinedClasses).toBe('base custom');
  });

  it('emits both click outputs without preventing router navigation', () => {
    const component = new RouterDropdownItemComponent();
    const click = vi.fn();
    const itemClick = vi.fn();
    const event = new MouseEvent('click');
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.click.subscribe(click);
    component.itemClick.subscribe(itemClick);

    component.handleClick();

    expect(preventDefault).not.toHaveBeenCalled();
    expect(click).toHaveBeenCalledTimes(1);
    expect(itemClick).toHaveBeenCalledTimes(1);
  });
});
