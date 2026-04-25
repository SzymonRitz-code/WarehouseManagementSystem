import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentPendingListComponent } from './document-pending-list.component';

describe('DocumentPendingListComponent', () => {
  let component: DocumentPendingListComponent;
  let fixture: ComponentFixture<DocumentPendingListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentPendingListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentPendingListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
