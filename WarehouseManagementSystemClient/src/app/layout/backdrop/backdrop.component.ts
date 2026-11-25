import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarStore } from '../../features/services/sidebar-store';

@Component({
  selector: 'app-backdrop',
  imports: [
    CommonModule,
  ],
  templateUrl: './backdrop.component.html',
})

export class BackdropComponent {
  readonly isMobileOpen;

  constructor(private sidebarStore: SidebarStore) {
    this.isMobileOpen = this.sidebarStore.isMobileOpen;
  }

  closeSidebar() {
    this.sidebarStore.setMobileOpen(false);
  }
}
