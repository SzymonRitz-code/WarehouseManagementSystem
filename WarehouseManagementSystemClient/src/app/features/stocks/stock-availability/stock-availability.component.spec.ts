import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockAvailabilityComponent} from './stock-availability.component';

describe('StockAvailabilityComponent', () => {
  let component: StockAvailabilityComponent;
  let fixture: ComponentFixture<StockAvailabilityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockAvailabilityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StockAvailabilityComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
