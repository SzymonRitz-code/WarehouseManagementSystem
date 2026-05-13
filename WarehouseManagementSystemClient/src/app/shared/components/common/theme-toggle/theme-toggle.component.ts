import { Component } from '@angular/core';
import { ThemeService } from '../../../../core/services/theme.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-theme-toggle',
  templateUrl: './theme-toggle.component.html',
  imports: [CommonModule],

})
export class ThemeToggleComponent {
  theme$;

  constructor(private themeService: ThemeService) {
    this.theme$ = this.themeService.theme$;
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }
}
