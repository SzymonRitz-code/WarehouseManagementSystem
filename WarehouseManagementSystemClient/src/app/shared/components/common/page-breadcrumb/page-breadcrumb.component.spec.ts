import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PageBreadcrumbComponent } from './page-breadcrumb.component';

describe('PageBreadcrumbComponent', () => {
  let component: PageBreadcrumbComponent;
  let fixture: ComponentFixture<PageBreadcrumbComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageBreadcrumbComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PageBreadcrumbComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
