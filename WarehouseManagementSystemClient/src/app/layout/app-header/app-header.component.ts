import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SidebarService } from '../../features/services/sidebar-service';
import { ThemeToggleComponent } from "../../shared/components/common/theme-toggle/theme-toggle.component";
import { UserDropdownComponent } from '../../shared/components/header/user-dropdown/user-dropdown.component';

@Component({
  selector: 'app-header',
  imports: [
    CommonModule,
    RouterModule,
    ThemeToggleComponent,
    UserDropdownComponent
],
  templateUrl: './app-header.component.html',
})
export class AppHeaderComponent {
  isApplicationMenuOpen = false;
  readonly isMobileOpen$;

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor(public sidebarService: SidebarService) {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
  }

  handleToggle() {
    if (window.innerWidth >= 1280) {
      this.sidebarService.toggleExpanded();
    } else {
      this.sidebarService.toggleMobileOpen();
    }
  }

  toggleApplicationMenu() {
    this.isApplicationMenuOpen = !this.isApplicationMenuOpen;
  }

  ngAfterViewInit() {
    document.addEventListener('keydown', this.handleKeyDown);
  }

  ngOnDestroy() {
    document.removeEventListener('keydown', this.handleKeyDown);
  }

  handleKeyDown = (event: KeyboardEvent) => {
    if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
      event.preventDefault();
      this.searchInput?.nativeElement.focus();
    }
  };
}

