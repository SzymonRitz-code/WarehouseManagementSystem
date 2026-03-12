import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-component-card',
  standalone:true,
  imports: [],
  templateUrl: './component-card.component.html',
  styles: ``
})
export class ComponentCardComponent {

  @Input() title!: string;
  @Input() desc: string = '';
  @Input() className: string = '';
}
