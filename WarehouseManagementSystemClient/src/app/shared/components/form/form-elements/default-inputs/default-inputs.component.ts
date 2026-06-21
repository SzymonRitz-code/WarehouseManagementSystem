import { Component } from '@angular/core';
import { ComponentCardComponent } from '../../../common/component-card/component-card.component';
import { LabelComponent } from '../../label/label.component';
import { InputFieldComponent } from '../../input/input-field.component';
import { DatePickerComponent } from '../../date-picker/date-picker.component';
import { TimePickerComponent } from '../../time-picker/time-picker.component';
import { SelectComponent } from '../../select/select.component';

@Component({
  selector: 'app-default-inputs',
  standalone:true,
  imports: [
    ComponentCardComponent,
    LabelComponent, 
    InputFieldComponent, 
    DatePickerComponent, 
    TimePickerComponent, 
    SelectComponent],
  templateUrl: './default-inputs.component.html'
})
export class DefaultInputsComponent {
  showPassword = false;
  options = [
    { value: 'marketing', label: 'Marketing' },
    { value: 'template', label: 'Template' },
    { value: 'development', label: 'Development' },
  ];
  selectedOption = '';
  dateValue: any;
  timeValue = '';
  cardNumber = '';

  handleSelectChange(value: string) {
    this.selectedOption = value;
  }

  handleDateChange(event: any) {
    this.dateValue = event;
  }

  handleTimeChange(event: any) {
    this.timeValue = event.target.value;
  }

  onTimeSelected(time: string) {
    this.timeValue = time;
  }
}
