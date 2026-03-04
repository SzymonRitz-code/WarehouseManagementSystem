import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdjustmentListComponent } from './adjustment-list.component';

describe('AdjustmentListComponent', () => {
  let component: AdjustmentListComponent;
  let fixture: ComponentFixture<AdjustmentListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdjustmentListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdjustmentListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
