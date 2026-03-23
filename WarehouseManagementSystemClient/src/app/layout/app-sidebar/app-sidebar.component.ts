import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, QueryList, ViewChildren } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { SafeHtmlPipe } from '../../pipe/safe-html.pipe';
import { combineLatest, Subscription } from 'rxjs';
import { SidebarService } from '../../features/services/sidebar-service';

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
  new?: boolean;
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


  // Main nav items
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
        { name: "Product Form", path: "/products/form" }  // create/edit
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
        { name: "Create Warehouse", path: "/warehouses/form" }  // dual create/edit form
        // opcjonalnie: { name: "Warehouse Zones", path: "/warehouses/zones" }
        // jeśli chcesz mieć globalną listę stref
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
        { name: "Create Zone", path: "/zones/form" }  // dual create/edit form
        // opcjonalnie: { name: "Warehouse Zones", path: "/warehouses/zones" }
        // jeśli chcesz mieć globalną listę stref
      ],
    },
    {
      name: "Stocks",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="3" width="18" height="18" stroke="currentColor" stroke-width="1.5"/>
            <rect x="7" y="7" width="10" height="10" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Stocks List", path: "/stocks" },
        { name: "Stock Availability", path: "/stocks/availability" },
        { name: "Stock Moves", path: "/stocks/move" }
        // rezerwacje i stock-move → dostępne w Stock Detail
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
        { name: "Document List", path: "/documents" },  // filtry status Draft/Confirmed/Transfer/Completed
        { name: "Document Form", path: "/documents/form" }  // create/edit
      ],
    },
    {
      name: "Inventory Adjustments",
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="4" width="18" height="16" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="8" x2="17" y2="8" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="12" x2="17" y2="12" stroke="currentColor" stroke-width="1.5"/>
            <line x1="7" y1="16" x2="17" y2="16" stroke="currentColor" stroke-width="1.5"/>
            </svg>`,
      subItems: [
        { name: "Adjustment List", path: "/adjustments" },
        { name: "Adjustment Form", path: "/adjustments/form" }  // create/edit
      ],
    },
    {
      name: "Users",
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="8" r="4" stroke="currentColor" stroke-width="1.5"/>
            <path d="M4 20C4 16 8 14 12 14C16 14 20 16 20 20" stroke="currentColor" stroke-width="1.5"/>
           </svg>`,
      subItems: [
        { name: "User List", path: "/users" },
        { name: "User Form", path: "/users/form" }  // create/edit
      ],
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
  // Others nav items
  othersItems: NavItem[] = [
    {
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M12 2C11.5858 2 11.25 2.33579 11.25 2.75V12C11.25 12.4142 11.5858 12.75 12 12.75H21.25C21.6642 12.75 22 12.4142 22 12C22 6.47715 17.5228 2 12 2ZM12.75 11.25V3.53263C13.2645 3.57761 13.7659 3.66843 14.25 3.80098V3.80099C15.6929 4.19606 16.9827 4.96184 18.0104 5.98959C19.0382 7.01734 19.8039 8.30707 20.199 9.75C20.3316 10.2341 20.4224 10.7355 20.4674 11.25H12.75ZM2 12C2 7.25083 5.31065 3.27489 9.75 2.25415V3.80099C6.14748 4.78734 3.5 8.0845 3.5 12C3.5 16.6944 7.30558 20.5 12 20.5C15.9155 20.5 19.2127 17.8525 20.199 14.25H21.7459C20.7251 18.6894 16.7492 22 12 22C6.47715 22 2 17.5229 2 12Z" fill="currentColor"></path></svg>`,
      name: "Charts",
      subItems: [
        { name: "Line Chart", path: "/line-chart", pro: false },
        { name: "Bar Chart", path: "/bar-chart", pro: false },
      ],
    },
    {
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M11.665 3.75618C11.8762 3.65061 12.1247 3.65061 12.3358 3.75618L18.7807 6.97853L12.3358 10.2009C12.1247 10.3064 11.8762 10.3064 11.665 10.2009L5.22014 6.97853L11.665 3.75618ZM4.29297 8.19199V16.0946C4.29297 16.3787 4.45347 16.6384 4.70757 16.7654L11.25 20.0365V11.6512C11.1631 11.6205 11.0777 11.5843 10.9942 11.5425L4.29297 8.19199ZM12.75 20.037L19.2933 16.7654C19.5474 16.6384 19.7079 16.3787 19.7079 16.0946V8.19199L13.0066 11.5425C12.9229 11.5844 12.8372 11.6207 12.75 11.6515V20.037ZM13.0066 2.41453C12.3732 2.09783 11.6277 2.09783 10.9942 2.41453L4.03676 5.89316C3.27449 6.27429 2.79297 7.05339 2.79297 7.90563V16.0946C2.79297 16.9468 3.27448 17.7259 4.03676 18.1071L10.9942 21.5857L11.3296 20.9149L10.9942 21.5857C11.6277 21.9024 12.3732 21.9024 13.0066 21.5857L19.9641 18.1071C20.7264 17.7259 21.2079 16.9468 21.2079 16.0946V7.90563C21.2079 7.05339 20.7264 6.27429 19.9641 5.89316L13.0066 2.41453Z" fill="currentColor"></path></svg>`,
      name: "UI Elements",
      subItems: [
        { name: "Alerts", path: "/alerts", pro: false },
        { name: "Avatar", path: "/avatars", pro: false },
        { name: "Badge", path: "/badge", pro: false },
        { name: "Buttons", path: "/buttons", pro: false },
        { name: "Images", path: "/images", pro: false },
        { name: "Videos", path: "/videos", pro: false },
      ],
    },
    {
      icon: `<svg width="1em" height="1em" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M14 2.75C14 2.33579 14.3358 2 14.75 2C15.1642 2 15.5 2.33579 15.5 2.75V5.73291L17.75 5.73291H19C19.4142 5.73291 19.75 6.0687 19.75 6.48291C19.75 6.89712 19.4142 7.23291 19 7.23291H18.5L18.5 12.2329C18.5 15.5691 15.9866 18.3183 12.75 18.6901V21.25C12.75 21.6642 12.4142 22 12 22C11.5858 22 11.25 21.6642 11.25 21.25V18.6901C8.01342 18.3183 5.5 15.5691 5.5 12.2329L5.5 7.23291H5C4.58579 7.23291 4.25 6.89712 4.25 6.48291C4.25 6.0687 4.58579 5.73291 5 5.73291L6.25 5.73291L8.5 5.73291L8.5 2.75C8.5 2.33579 8.83579 2 9.25 2C9.66421 2 10 2.33579 10 2.75L10 5.73291L14 5.73291V2.75ZM7 7.23291L7 12.2329C7 14.9943 9.23858 17.2329 12 17.2329C14.7614 17.2329 17 14.9943 17 12.2329L17 7.23291L7 7.23291Z" fill="currentColor"></path></svg>`,
      name: "Authentication",
      subItems: [
        { name: "Sign In", path: "/signin", pro: false },
        { name: "Sign Up", path: "/signup", pro: false },
      ],
    },
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
    // Subscribe to router events
    this.subscription.add(
      this.router.events.subscribe(event => {
        if (event instanceof NavigationEnd) {
          this.setActiveMenuFromRoute(this.router.url);
        }
      })
    );

    // Subscribe to combined observables to close submenus when all are false
    this.subscription.add(
      combineLatest([this.isExpanded$, this.isMobileOpen$, this.isHovered$]).subscribe(
        ([isExpanded, isMobileOpen, isHovered]) => {
          if (!isExpanded && !isMobileOpen && !isHovered) {
            // this.openSubmenu = null;
            // this.savedSubMenuHeights = { ...this.subMenuHeights };
            // this.subMenuHeights = {};
            this.cdr.detectChanges();
          } else {
            // Restore saved heights when reopening
            // this.subMenuHeights = { ...this.savedSubMenuHeights };
            // this.cdr.detectChanges();
          }
        }
      )
    );

    // Initial load
    this.setActiveMenuFromRoute(this.router.url);
  }

  ngOnDestroy() {
    // Clean up subscriptions
    this.subscription.unsubscribe();
  }

  isActive(path: string): boolean {
    return this.router.url === path;
  }

  // toggleSubmenu(section: string, index: number) {
  //   const key = `${section}-${index}`;

  //   if (this.openSubmenu === key) {
  //     this.openSubmenu = null;
  //     this.subMenuHeights[key] = 0;
  //   } else {
  //     this.openSubmenu = key;

  //     setTimeout(() => {
  //       const el = document.getElementById(key);
  //       if (el) {
  //         this.subMenuHeights[key] = el.scrollHeight;
  //         this.cdr.detectChanges(); // Ensure UI updates
  //       }
  //     });
  //   }
  // }
  toggleSubmenu(section: string, index: number) {
    const key = `${section}-${index}`;

    // jeśli kliknięty element jest już otwarty -> zamknij
    if (this.openSubmenu === key) {
      this.openSubmenu = null;
      this.subMenuHeights[key] = 0;
    } else {
      // zamknij poprzednie submenu
      if (this.openSubmenu) {
        this.subMenuHeights[this.openSubmenu] = 0;
      }

      this.openSubmenu = key;

      // ustaw wysokość po chwili, żeby scrollHeight był poprawny
      setTimeout(() => {
        const el = document.getElementById(key);
        if (el) {
          // scrollHeight może być zerowy jeśli element ma display:none
          // dlatego tymczasowo ustawiamy height auto, odczytujemy scrollHeight i wracamy
          const prevHeight = el.style.height;
          el.style.height = 'auto';
          this.subMenuHeights[key] = el.scrollHeight;
          el.style.height = prevHeight;
          this.cdr.detectChanges(); // wymuszenie update UI
        }
      });
    }
  }

  onSidebarMouseEnter() {
    this.isExpanded$.subscribe(expanded => {
      if (!expanded) {
        this.sidebarService.setHovered(true);
      }
    }).unsubscribe();
  }

  private setActiveMenuFromRoute(currentUrl: string) {
    const menuGroups = [
      { items: this.navItems, prefix: 'main' },
      { items: this.othersItems, prefix: 'others' },
    ];

    menuGroups.forEach(group => {
      group.items.forEach((nav, i) => {
        if (nav.subItems) {
          nav.subItems.forEach(subItem => {
            if (currentUrl === subItem.path) {
              const key = `${group.prefix}-${i}`;
              this.openSubmenu = key;

              setTimeout(() => {
                const el = document.getElementById(key);
                if (el) {
                  this.subMenuHeights[key] = el.scrollHeight;
                  this.cdr.detectChanges(); // Ensure UI updates
                }
              });
            }
          });
        }
      });
    });
  }

  onSubmenuClick() {
    console.log('click submenu');
    this.isMobileOpen$.subscribe(isMobile => {
      if (isMobile) {
        this.sidebarService.setMobileOpen(false);
      }
    }).unsubscribe();
  }
}
