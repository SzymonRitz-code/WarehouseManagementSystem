import { DropdownItemTwoComponent } from './dropdown-item.component-two';

describe('DropdownItemTwoComponent', () => {
  it('combines base and custom classes', () => {
    const component = new DropdownItemTwoComponent();
    component.baseClassName = 'base';
    component.className = 'custom';

    expect(component.combinedClasses).toBe('base custom');
  });

  it('emits both click outputs without preventing router navigation', () => {
    const component = new DropdownItemTwoComponent();
    const click = vi.fn();
    const itemClick = vi.fn();
    const event = new MouseEvent('click');
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.click.subscribe(click);
    component.itemClick.subscribe(itemClick);

    component.handleClick(event);

    expect(preventDefault).not.toHaveBeenCalled();
    expect(click).toHaveBeenCalledTimes(1);
    expect(itemClick).toHaveBeenCalledTimes(1);
  });
});
