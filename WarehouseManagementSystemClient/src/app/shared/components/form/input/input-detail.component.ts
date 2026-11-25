import { Component, computed, input } from "@angular/core";

@Component({
  selector: 'app-input-detail',
  template: `
    <input 
      [value]="displayValue()" 
      disabled
      [class]="'ui-control-readonly ' + className()"
    />
  `
})
export class InputDetailComponent {
  readonly value = input<unknown>();
  readonly type = input<'text' | 'boolean' | 'date' | 'enum'>('text');
  readonly className = input('');

  readonly displayValue = computed(() => {
    const value = this.value();
    if (typeof value === 'boolean') {
      return value ? 'Yes' : 'No';
    }

    if (value instanceof Date) {
      return value.toDateString();
    }

    return value == null ? '' : String(value);
  });
}
