import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FileInputComponent } from './file-input.component';

describe('FileInputComponent', () => {
  let component: FileInputComponent;
  let fixture: ComponentFixture<FileInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FileInputComponent] }).compileComponents();
    fixture = TestBed.createComponent(FileInputComponent);
    component = fixture.componentInstance;
  });

  it('emits the original change event', () => {
    const change = vi.fn();
    const event = new Event('change');
    component.change.subscribe(change);

    component.onChange(event);

    expect(change).toHaveBeenCalledWith(event);
  });
});
