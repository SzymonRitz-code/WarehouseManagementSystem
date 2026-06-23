import { of } from 'rxjs';
import { UserDropdownComponent } from './user-dropdown.component';

describe('UserDropdownComponent', () => {
  it('toggles dropdown open state', () => {
    const component = createComponent();

    component.toggleDropdown();
    expect(component.isOpen).toBe(true);

    component.toggleDropdown();
    expect(component.isOpen).toBe(false);
  });

  it('closes dropdown explicitly', () => {
    const component = createComponent();
    component.isOpen = true;

    component.closeDropdown();

    expect(component.isOpen).toBe(false);
  });

  it('starts OIDC sign-out flow', () => {
    const oidc = {
      logoffAndRevokeTokens: vi.fn().mockReturnValue(of(null))
    };
    const component = new UserDropdownComponent(oidc as any);

    component.signOut();

    expect(oidc.logoffAndRevokeTokens).toHaveBeenCalledTimes(1);
  });

  function createComponent(): UserDropdownComponent {
    return new UserDropdownComponent({
      logoffAndRevokeTokens: vi.fn().mockReturnValue(of(null))
    } as any);
  }
});
