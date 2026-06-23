import { NavigationEnd } from '@angular/router';
import { Subject } from 'rxjs';
import { SidebarService } from '../../features/services/sidebar-service';
import { AppSidebarComponent } from './app-sidebar.component';

describe('AppSidebarComponent', () => {
  let component: AppSidebarComponent;
  let routerEvents$: Subject<NavigationEnd>;
  let router: { url: string; events: Subject<NavigationEnd> };

  beforeEach(() => {
    routerEvents$ = new Subject<NavigationEnd>();
    router = {
      url: '/home',
      events: routerEvents$
    };

    component = new AppSidebarComponent(
      new SidebarService(),
      router as any,
      { detectChanges: vi.fn() } as any
    );
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('clears submenu active state when navigating from submenu route to a direct menu route', () => {
    (component as any).setActiveMenuFromRoute('/documents');
    expect(component.openSubmenu).toBe('main-5');

    (component as any).setActiveMenuFromRoute('/users');

    expect(component.openSubmenu).toBeNull();
    expect(component.subMenuHeights['main-5']).toBe(0);
  });

  it('updates active submenu from router navigation events', () => {
    component.ngOnInit();
    router.url = '/documents/pending';

    routerEvents$.next(new NavigationEnd(1, '/documents/pending', '/documents/pending'));

    expect(component.openSubmenu).toBe('main-5');
  });
});
