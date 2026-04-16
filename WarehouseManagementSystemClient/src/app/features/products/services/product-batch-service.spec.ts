import { TestBed } from '@angular/core/testing';

import { ProductBatchService } from './product-batch-service';

describe('ProductBatchService', () => {
  let service: ProductBatchService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProductBatchService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
