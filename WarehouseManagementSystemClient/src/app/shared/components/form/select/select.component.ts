
import { Component, input, linkedSignal, output, OnInit } from '@angular/core';

export interface Option {
  value: string;
  label: string;
}

@Component({
  selector: 'app-select',
  standalone:true,
  imports: [],
  templateUrl: './select.component.html',
})
export class SelectComponent implements OnInit {
  readonly options = input<Option[]>([]);
  readonly placeholder = input('Select an option');
  readonly className = input('');
  readonly defaultValue = input('');
  readonly value = input('');
  readonly valueState = linkedSignal(() => this.value() || this.defaultValue());

  readonly valueChange = output<string>();

  ngOnInit() {
    if (!this.valueState() && this.defaultValue()) {
      this.valueState.set(this.defaultValue());
    }
  }

  onChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.valueState.set(value);
    this.valueChange.emit(value);
  }
}