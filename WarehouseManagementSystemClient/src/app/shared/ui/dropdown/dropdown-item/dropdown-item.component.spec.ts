import { DropdownItemComponent } from './dropdown-item.component';

describe('DropdownItemComponent', () => {
  it('combines base and custom classes without losing defaults', () => {
    const component = new DropdownItemComponent();
    component.baseClassName = 'base';
    component.className = 'custom';

    expect(component.combinedClasses).toBe('base custom');
  });

  it('prevents default navigation and emits both click outputs', () => {
    const component = new DropdownItemComponent();
    const click = vi.fn();
    const itemClick = vi.fn();
    const event = new MouseEvent('click');
    const preventDefault = vi.spyOn(event, 'preventDefault');
    component.click.subscribe(click);
    component.itemClick.subscribe(itemClick);

    component.handleClick(event);

    expect(preventDefault).toHaveBeenCalledTimes(1);
    expect(click).toHaveBeenCalledTimes(1);
    expect(itemClick).toHaveBeenCalledTimes(1);
  });
});
