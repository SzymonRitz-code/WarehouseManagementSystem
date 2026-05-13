import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DropdownComponent } from '../../../ui/dropdown/dropdown.component';
import { DropdownItemTwoComponent } from '../../../ui/dropdown/dropdown-item/dropdown-item.component-two';

@Component({
  selector: 'app-user-dropdown',
  templateUrl: './user-dropdown.component.html',
  imports:[CommonModule,RouterModule,DropdownComponent, DropdownItemTwoComponent]
})
export class UserDropdownComponent {
  isOpen = false;

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }

  closeDropdown() {
    this.isOpen = false;
  }
}