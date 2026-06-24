import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DropdownComponent } from '../../../ui/dropdown/dropdown.component';
import { RouterDropdownItemComponent } from '../../../ui/dropdown/dropdown-item/router-dropdown-item.component';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-user-dropdown',
  templateUrl: './user-dropdown.component.html',
  imports: [CommonModule, RouterModule, DropdownComponent, RouterDropdownItemComponent]
})
export class UserDropdownComponent {
  isOpen = false;

  constructor(private oidcSecurityService: OidcSecurityService) {}

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  closeDropdown() {
    this.isOpen = false;
  }
  signOut() {
    this.oidcSecurityService.logoffAndRevokeTokens().subscribe();
  }
}
