import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductBatchFormComponent } from './product-batch-form.component';

describe('ProductBatchFormComponent', () => {
  let component: ProductBatchFormComponent;
  let fixture: ComponentFixture<ProductBatchFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductBatchFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductBatchFormComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
