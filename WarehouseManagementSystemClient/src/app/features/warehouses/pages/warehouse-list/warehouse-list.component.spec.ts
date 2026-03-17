import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WarehouseListComponent } from './warehouse-list.component';

describe('WarehouseListComponent', () => {
  let component: WarehouseListComponent;
  let fixture: ComponentFixture<WarehouseListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WarehouseListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WarehouseListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
