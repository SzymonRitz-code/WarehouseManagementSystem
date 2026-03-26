import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdjustmentCreateComponent } from './adjustment-form.component';

describe('AdjustmentCreateComponent', () => {
  let component: AdjustmentCreateComponent;
  let fixture: ComponentFixture<AdjustmentCreateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdjustmentCreateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdjustmentCreateComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
