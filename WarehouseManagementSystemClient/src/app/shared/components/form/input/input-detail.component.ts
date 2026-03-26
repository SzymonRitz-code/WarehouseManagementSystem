import { Component, Input } from "@angular/core";

@Component({
  selector: 'app-input-detail',
  template: `
    <input 
      [value]="displayValue" 
      disabled 
      class="h-11 w-full rounded border px-4 py-2 text-sm" 
    />
  `
})
export class InputDetailComponent {
  @Input() value: any;
  @Input() type: 'text' | 'boolean' | 'date' | 'enum' = 'text';

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