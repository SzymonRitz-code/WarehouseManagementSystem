import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { BackdropComponent } from '../backdrop/backdrop.component';
import { SidebarStore } from '../../features/services/sidebar-store';

@Component({
  selector: 'app-layout',
  imports: [
    CommonModule,
    RouterModule,
    AppHeaderComponent,
    AppSidebarComponent,
    BackdropComponent
  ],
  templateUrl: './app-layout.component.html',
})

export class AppLayoutComponent {
  readonly isExpanded;
  readonly isHovered;
  readonly isMobileOpen;

  constructor(public sidebarStore: SidebarStore) {
    this.isExpanded = this.sidebarStore.isExpanded;
    this.isHovered = this.sidebarStore.isHovered;
    this.isMobileOpen = this.sidebarStore.isMobileOpen;
  }

  get containerClasses() {
    return [
      'flex-1',
      'transition-all',
      'duration-300',
      'ease-in-out',
      (this.isExpanded() || this.isHovered()) ? 'xl:ml-[290px]' : 'xl:ml-[90px]',
      this.isMobileOpen() ? 'ml-0' : ''
    ];
  }

}