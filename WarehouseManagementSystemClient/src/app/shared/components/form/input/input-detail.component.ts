import { Component, Input } from "@angular/core";

@Component({
  selector: 'app-input-detail',
  template: `
    <input 
      [value]="displayValue" 
      disabled
      [class]="'ui-control-readonly ' + className"
    />
  `
})
export class InputDetailComponent {
  @Input() value: any;
  @Input() type: 'text' | 'boolean' | 'date' | 'enum' = 'text';
  @Input() className = '';

  get displayValue(): string {
    if (typeof this.value === 'boolean') {
      return this.value ? 'Yes' : 'No';
    }

    if (this.value instanceof Date) {
      return this.value.toDateString();
    }

    return this.value ?? '';
  }
}
