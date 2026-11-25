import { Component, input } from '@angular/core';

@Component({
  selector: 'app-component-card',
  standalone:true,
  imports: [],
  templateUrl: './component-card.component.html',
  styles: ``
})
export class ComponentCardComponent {
  readonly title = input('');
  readonly desc = input('');
  readonly className = input('');
}
