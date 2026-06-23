import { TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { SafeHtmlPipe } from './safe-html.pipe';

describe('SafeHtmlPipe', () => {
  it('delegates trusted HTML creation to Angular DomSanitizer', () => {
    const sanitizer = {
      bypassSecurityTrustHtml: vi.fn().mockReturnValue('trusted-html')
    } as unknown as DomSanitizer;
    const pipe = new SafeHtmlPipe(sanitizer);

    const result = pipe.transform('<strong>safe fragment</strong>');

    expect(sanitizer.bypassSecurityTrustHtml).toHaveBeenCalledWith('<strong>safe fragment</strong>');
    expect(result).toBe('trusted-html' as any);
  });

  it('can be created through Angular injection', () => {
    TestBed.configureTestingModule({
      providers: [SafeHtmlPipe]
    });

    expect(TestBed.inject(SafeHtmlPipe)).toBeTruthy();
  });
});
