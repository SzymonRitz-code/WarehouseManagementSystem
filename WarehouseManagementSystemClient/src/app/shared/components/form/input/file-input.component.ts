
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-file-input',
  imports: [],
  template: `
    <input
      type="file"
      [class]="
        'ui-control w-full overflow-hidden text-gray-500 file:mr-5 file:border-collapse file:cursor-pointer file:rounded-l-lg file:border-0 file:border-r file:border-solid file:border-gray-200 file:bg-gray-50 file:py-3 file:pl-3.5 file:pr-3 file:text-sm file:text-gray-700 hover:file:bg-gray-100 focus:file:ring-brand-300 dark:text-gray-400 dark:file:border-gray-800 dark:file:bg-white/[0.03] dark:file:text-gray-400 ' + className
      "
      (change)="onChange($event)"
    />
  `,
  styles: ``
})
export class FileInputComponent {

  @Input() className: string = '';
  @Output() change = new EventEmitter<Event>();

  onChange(event: Event) {
    this.change.emit(event);
  }
}
