import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SidebarStore } from '../../features/services/sidebar-store';
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
  readonly isMobileOpen;

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor(public sidebarStore: SidebarStore) {
    this.isMobileOpen = this.sidebarStore.isMobileOpen;
  }

  handleToggle() {
    if (window.innerWidth >= 1280) {
      this.sidebarStore.toggleExpanded();
    } else {
      this.sidebarStore.toggleMobileOpen();
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

