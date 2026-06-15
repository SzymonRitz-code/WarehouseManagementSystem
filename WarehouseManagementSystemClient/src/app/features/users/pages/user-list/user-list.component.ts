import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { User } from '../../model/user';
import { TableComponent } from "../../../../shared/components/table/table.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { UserService } from '../../../services/user-service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, TableComponent, ComponentCardComponent, PageBreadcrumbComponent],
  templateUrl: './user-list.component.html'
})
export class UserListComponent implements OnInit {

  users$!: Observable<User[]>;
  constructor(private router: Router, private userService: UserService) { }


  ngOnInit(): void {
      this.users$ = this.userService.getUsers();
  }


  columns = [
    { key: 'id', label: 'ID', sortable: true },                  // unikalny identyfikator użytkownika
    { key: 'username', label: 'Username', sortable: true },      // login lub nazwa użytkownika
    { key: 'firstName', label: 'First Name', sortable: true },
    { key: 'lastName', label: 'Last Name', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'role', label: 'Role', sortable: true },              // np. Admin, User, Moderator
    { key: 'status', label: 'Status', sortable: true, type: 'boolean' } // aktywny / nieaktywny
  ];

  userActions = [
    { label: 'Edit', action: 'edit' },
    { label: 'Details', action: 'details' },
  ];
  goToForm() {
    this.router.navigate(['/users/form']);
  }
  onUserAction(event: { row: User; action: string }) {
    const { row, action } = event;

    switch (action) {
      case 'edit':
        this.onEdit(row);
        break;

      case 'details':
        this.onDetails(row);
        break;
    }
  }
  onDetails(user: User) {
    this.router.navigateByUrl(`/users/detail/${user.id}`)
  }
  onEdit(user: User) {
    this.router.navigateByUrl(`/users/form/${user.id}`)
  }
}
