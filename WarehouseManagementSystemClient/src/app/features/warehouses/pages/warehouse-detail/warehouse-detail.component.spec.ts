import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WarehouseDetailComponent } from './warehouse-detail.component';

describe('WarehouseDetailComponent', () => {
  let component: WarehouseDetailComponent;
  let fixture: ComponentFixture<WarehouseDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WarehouseDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WarehouseDetailComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
