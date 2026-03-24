import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentItemsDetailComponent } from './app-document-items-detail.component';

describe('AppDocumentItemsDetailComponent', () => {
  let component: DocumentItemsDetailComponent;
  let fixture: ComponentFixture<DocumentItemsDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentItemsDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentItemsDetailComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
