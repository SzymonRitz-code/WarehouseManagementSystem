import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputFieldComponent } from "../../../../shared/components/form/input/input-field.component";
import { FormActionsComponent } from "../../../../shared/components/form/form-actions/form-actions.component";
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../../../services/user-service';
import { CommonModule } from '@angular/common';
import { User } from '../../model/user';
import { CreateUser } from '../../model/create-user';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    PageBreadcrumbComponent,
    ComponentCardComponent,
    LabelComponent,
    InputFieldComponent,
    FormActionsComponent],
  templateUrl: './user-form.component.html'
})
export class UserFormComponent implements OnInit {

  userForm!: FormGroup;
  id!: string;
  user!: User | CreateUser;

  constructor(private router: Router, private fb: FormBuilder, private activatedRoute: ActivatedRoute, private userService: UserService) { }

  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.userForm = this.fb.nonNullable.group({
      id: [''],
      username: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      role: ['', Validators.required],
      status: ['', Validators.required]
    });
    if (this.id) {
      const cachedUser = this.userService.getUser(this.id);

      if (cachedUser) {
        this.user = cachedUser;
        this.patchFormFromUser(cachedUser);
        return;
      }

      this.userService.getUsers().subscribe(users => {
        const loadedUser = users.find(user => user.id === this.id);
        if (!loadedUser) {
          this.router.navigateByUrl('/users');
          return;
        }

        this.user = loadedUser;
        this.patchFormFromUser(loadedUser);
      });
    }
  }

  private patchFormFromUser(user: User) {
    this.userForm.patchValue({
      id: user.id,
      username: user.username,
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      role: user.role,
      status: user.status
    });
  }

  onSave() {
    this.user = this.userForm.value
    if(this.id === null){
    this.user = this.userService.addUser(this.user) as User;
    }
    this.router.navigateByUrl(`/users/detail/${(this.user as User).id}`);
  }
  onBack() {
    this.router.navigateByUrl('/users');
  }

}
