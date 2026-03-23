import { Component, Input } from "@angular/core";

@Component({
  selector: 'app-input-detail',
  template: `<input [value]="value ?? ''" [disabled]="true" class="h-11 w-full rounded border px-4 py-2 text-sm" />`
})
export class InputDetailComponent {
  @Input() value: any;
}