
import { Component, OnInit } from '@angular/core';
import { LabelComponent } from '../../form/label/label.component';
import { InputFieldComponent } from '../../form/input/input-field.component';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonComponent } from '../../../ui/button/button.component';

@Component({
  selector: 'app-signin-form',
  templateUrl: './signin-form.component.html',
  imports: [
    LabelComponent,
    ButtonComponent,
    InputFieldComponent,
    RouterModule,
    FormsModule,
    ReactiveFormsModule // formControlName requires ReactiveFormsModule
  ]
})
export class SigninFormComponent implements OnInit {


  signInForm!: FormGroup
  showPassword = false;

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.signInForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', [Validators.required]]
    })
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  onSignIn() {
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
    }
  }
}
