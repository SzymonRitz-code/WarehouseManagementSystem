import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductBatchListComponent } from './product-batch-list.component';

describe('ProductBatchListComponent', () => {
  let component: ProductBatchListComponent;
  let fixture: ComponentFixture<ProductBatchListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductBatchListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductBatchListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
