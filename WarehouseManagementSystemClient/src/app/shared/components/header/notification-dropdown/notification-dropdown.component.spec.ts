import { NotificationDropdownComponent } from './notification-dropdown.component';

describe('NotificationDropdownComponent', () => {
  it('opens dropdown and marks notifications as seen on toggle', () => {
    const component = new NotificationDropdownComponent();

    component.toggleDropdown();

    expect(component.isOpen).toBe(true);
    expect(component.notifying).toBe(false);
  });

  it('toggles dropdown open state on repeated clicks', () => {
    const component = new NotificationDropdownComponent();

    component.toggleDropdown();
    component.toggleDropdown();

    expect(component.isOpen).toBe(false);
  });

  it('closes dropdown explicitly', () => {
    const component = new NotificationDropdownComponent();
    component.isOpen = true;

    component.closeDropdown();

    expect(component.isOpen).toBe(false);
  });
});
