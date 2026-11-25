import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-validation-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './validation-summary.component.html'
})
export class ValidationSummaryComponent {
  readonly form = input.required<FormGroup>();
}
