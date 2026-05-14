import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-signout-callback',
  standalone: true,
  templateUrl: './signout-callback.component.html'
})
export class SignoutCallbackComponent implements OnInit {

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.router.navigateByUrl('');
  }
}