import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-signin-callback',
  standalone: true,
  template: ''
})
export class SigninCallbackComponent implements OnInit {
  constructor(
    private oidcSecurityService: OidcSecurityService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Complete the authorization-code exchange before leaving the callback route.
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated }) => {
      this.router.navigateByUrl(isAuthenticated ? '' : '/signin');
    });
  }
}
