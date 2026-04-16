import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductBatchDetailComponent } from './product-batch-detail.component';

describe('ProductBatchDetailComponent', () => {
  let component: ProductBatchDetailComponent;
  let fixture: ComponentFixture<ProductBatchDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductBatchDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductBatchDetailComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
