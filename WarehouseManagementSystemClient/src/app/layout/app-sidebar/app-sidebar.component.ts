import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, QueryList, ViewChildren } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { SidebarService } from '../../features/services/sidebar-service';
import { SafeHtmlPipe } from '../../pipe/safe-html.pipe';

interface NavSubItem {
  name: string;
  path: string;
  new?: boolean;
  pro?: boolean;
}

interface NavItem {
  name: string;
  icon: string;
  path?: string;
  subItems?: NavSubItem[];
}

@Component({
  selector: 'app-sidebar',
  imports: [
    CommonModule,
    RouterModule,
    SafeHtmlPipe
  ],
  templateUrl: './app-sidebar.component.html',
})
export class AppSidebarComponent {
  navItems: NavItem[] = [
    {
      name: "Dashboard",
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none">
            <path d="M3 10L12 3L21 10V21H3V10Z" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      path: "/home",
    },
    {
      name: "Products",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <path d="M3 8l9-5 9 5v10l-9 5-9-5V8z" stroke="currentColor" stroke-width="1.5"/>
            <path d="M12 3v18" stroke="currentColor" stroke-width="1.5"/>
            <path d="M3 8l9 5 9-5" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Product List", path: "/products" },
        { name: "Product Form", path: "/products/form" }
      ],
    },
    {
      name: "Warehouses",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M3 9L12 2l9 7v11a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V9z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M9 22V12h6v10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>`,
      subItems: [
        { name: "Warehouse List", path: "/warehouses" },
        { name: "Create Warehouse", path: "/warehouses/form" }
      ],
    },
    {
      name: "Zones",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="3" width="8" height="8" stroke="currentColor" stroke-width="1.5"/>
            <rect x="13" y="3" width="8" height="8" stroke="currentColor" stroke-width="1.5"/>
            <rect x="3" y="13" width="8" height="8" stroke="currentColor" stroke-width="1.5"/>
            <rect x="13" y="13" width="8" height="8" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Zone List", path: "/zones" },
        { name: "Create Zone", path: "/zones/form" }
      ],
    },
    {
      name: "Stocks",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="4" width="18" height="16" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="8" x2="17" y2="8" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="12" x2="17" y2="12" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="16" x2="17" y2="16" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Stocks List", path: "/stocks" },
        { name: "Stock Availability", path: "/stocks/availability" }
      ],
    },
    {
      name: "Documents",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <path d="M4 2h16v20H4V2z" stroke="currentColor" stroke-width="1.5"/>
            <path d="M4 7h16" stroke="currentColor" stroke-width="1.5"/>
            <path d="M4 12h16" stroke="currentColor" stroke-width="1.5"/>
            <path d="M4 17h10" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Document List", path: "/documents" },
        { name: "Pending Documents", path: "/documents/pending" },
        { name: "Document Form", path: "/documents/form" }
      ],
    },
    {
      name: "Users",
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="8" r="4" stroke="currentColor" stroke-width="1.5"/>
            <path d="M4 20C4 16 8 14 12 14C16 14 20 16 20 20" stroke="currentColor" stroke-width="1.5"/>
           </svg>`,
      path: '/users'
    },
    {
      name: "Audit",
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none">
            <path d="M12 15.5C13.933 15.5 15.5 13.933 15.5 12C15.5 10.067 13.933 8.5 12 8.5C10.067 8.5 8.5 10.067 8.5 12C8.5 13.933 10.067 15.5 12 15.5Z" stroke="currentColor" stroke-width="1.5"/>
            <path d="M19.4 15A7.975 7.975 0 0 0 20 12C20 11.3 19.9 10.6 19.8 9.9M4.6 9A7.975 7.975 0 0 0 4 12C4 12.7 4.1 13.4 4.2 14.1" stroke="currentColor" stroke-width="1.5"/>
           </svg>`,
      path: '/audit'
    }
  ];

  openSubmenu: string | null | number = null;
  subMenuHeights: { [key: string]: number } = {};
  @ViewChildren('subMenu') subMenuRefs!: QueryList<ElementRef>;

  readonly isExpanded$;
  readonly isMobileOpen$;
  readonly isHovered$;

  private subscription: Subscription = new Subscription();

  constructor(
    public sidebarService: SidebarService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.isExpanded$ = this.sidebarService.isExpanded$;
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    this.isHovered$ = this.sidebarService.isHovered$;
  }

  ngOnInit() {
    this.subscription.add(
      this.router.events.subscribe(event => {
        if (event instanceof NavigationEnd) {
          this.setActiveMenuFromRoute(this.router.url);
        }
      })
    );

    this.subscription.add(
      combineLatest([this.isExpanded$, this.isMobileOpen$, this.isHovered$]).subscribe(
        ([isExpanded, isMobileOpen, isHovered]) => {
          if (!isExpanded && !isMobileOpen && !isHovered) {
            this.cdr.detectChanges();
          }
        }
      )
    );

    this.setActiveMenuFromRoute(this.router.url);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  isActive(path: string): boolean {
    return this.router.url === path;
  }

  toggleSubmenu(section: string, index: number) {
    const key = `${section}-${index}`;

    if (this.openSubmenu === key) {
      this.openSubmenu = null;
      this.subMenuHeights[key] = 0;
      return;
    }

    if (this.openSubmenu) {
      this.subMenuHeights[this.openSubmenu] = 0;
    }

    this.openSubmenu = key;

    setTimeout(() => {
      const el = document.getElementById(key);
      if (el) {
        const prevHeight = el.style.height;
        el.style.height = 'auto';
        this.subMenuHeights[key] = el.scrollHeight;
        el.style.height = prevHeight;
        this.cdr.detectChanges();
      }
    });
  }

  onSidebarMouseEnter() {
    this.isExpanded$.subscribe(expanded => {
      if (!expanded) {
        this.sidebarService.setHovered(true);
      }
    }).unsubscribe();
  }

  private setActiveMenuFromRoute(currentUrl: string) {
    this.navItems.forEach((nav, i) => {
      if (!nav.subItems) {
        return;
      }

      nav.subItems.forEach(subItem => {
        if (currentUrl === subItem.path) {
          const key = `main-${i}`;
          this.openSubmenu = key;

          setTimeout(() => {
            const el = document.getElementById(key);
            if (el) {
              this.subMenuHeights[key] = el.scrollHeight;
              this.cdr.detectChanges();
            }
          });
        }
      });
    });
  }

  onSubmenuClick() {
    this.isMobileOpen$.subscribe(isMobile => {
      if (isMobile) {
        this.sidebarService.setMobileOpen(false);
      }
    }).unsubscribe();
  }
}
