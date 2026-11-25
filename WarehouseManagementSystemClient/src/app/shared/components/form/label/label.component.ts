import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-label',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './label.component.html',
  styles: ``
})
export class LabelComponent {
  readonly for = input<string>();
  readonly className = input('');
}
