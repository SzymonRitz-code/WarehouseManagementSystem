import { TestBed } from '@angular/core/testing';
import { take } from 'rxjs';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.className = '';
    document.body.className = '';
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.className = '';
    document.body.className = '';
  });

  it('starts with light theme when no saved theme exists', async () => {
    const service = TestBed.inject(ThemeService);

    await expect(firstTheme(service)).resolves.toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    expect(document.body.classList.contains('dark:bg-gray-900')).toBe(false);
  });

  it('restores saved dark theme on construction', async () => {
    localStorage.setItem('theme', 'dark');

    const service = TestBed.inject(ThemeService);

    await expect(firstTheme(service)).resolves.toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(document.body.classList.contains('dark:bg-gray-900')).toBe(true);
  });

  it('sets dark theme and persists DOM state', async () => {
    const service = TestBed.inject(ThemeService);

    service.setTheme('dark');

    await expect(firstTheme(service)).resolves.toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(document.body.classList.contains('dark:bg-gray-900')).toBe(true);
  });

  it('toggles theme between light and dark', async () => {
    const service = TestBed.inject(ThemeService);

    service.toggleTheme();
    await expect(firstTheme(service)).resolves.toBe('dark');

    service.toggleTheme();
    await expect(firstTheme(service)).resolves.toBe('light');
  });

  function firstTheme(service: ThemeService): Promise<string> {
    return new Promise(resolve => service.theme$.pipe(take(1)).subscribe(theme => resolve(theme)));
  }
});
