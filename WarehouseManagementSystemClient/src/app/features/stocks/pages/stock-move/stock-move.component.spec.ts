import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockMoveComponent } from './stock-move.component';

describe('StockMoveComponent', () => {
  let component: StockMoveComponent;
  let fixture: ComponentFixture<StockMoveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockMoveComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StockMoveComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
