import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentItemsFormComponent } from './document-items-form.component';

describe('DocumentItemsFormComponent', () => {
  let component: DocumentItemsFormComponent;
  let fixture: ComponentFixture<DocumentItemsFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentItemsFormComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentItemsFormComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
