import { TestBed } from '@angular/core/testing';
import { SidebarStore } from './sidebar-store';

describe('SidebarStore', () => {
  let store: SidebarStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(SidebarStore);
  });

  it('should be created', () => {
    expect(store).toBeTruthy();
  });

  it('updates desktop and mobile sidebar state', () => {
    store.toggleExpanded();
    store.toggleMobileOpen();
    store.setHovered(true);

    expect(store.isExpanded()).toBe(false);
    expect(store.isMobileOpen()).toBe(true);
    expect(store.isHovered()).toBe(true);
  });
});
