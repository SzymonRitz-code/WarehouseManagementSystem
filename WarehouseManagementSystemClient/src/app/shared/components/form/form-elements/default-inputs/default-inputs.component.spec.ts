import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DefaultInputsComponent } from './default-inputs.component';

describe('DefaultInputsComponent', () => {
  let component: DefaultInputsComponent;
  let fixture: ComponentFixture<DefaultInputsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DefaultInputsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DefaultInputsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
