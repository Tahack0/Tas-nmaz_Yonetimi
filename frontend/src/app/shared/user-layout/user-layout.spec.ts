import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { Auth } from '../../core/auth';
import { UserLayout } from './user-layout';

@Component({ template: '<p>Page content</p>' })
class TestPage {}

describe('UserLayout navigation', () => {
  beforeEach(() => {
    localStorage.removeItem('dashboard-sidebar-collapsed');
    TestBed.configureTestingModule({
      providers: [
        { provide: Auth, useValue: {
          getEmail: () => 'user@example.com',
          getRole: () => 'User',
          logout: () => of(undefined),
        } },
        provideRouter([
          { path: 'login', component: TestPage },
          { path: '', component: UserLayout, children:
            ['dashboard', 'spatial-analysis', 'properties', 'property-map']
              .map(path => ({ path, component: TestPage })),
          },
        ]),
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem('dashboard-sidebar-collapsed');
    vi.restoreAllMocks();
  });

  it('keeps the sidebar and its saved preference across all four pages', async () => {
    localStorage.setItem('dashboard-sidebar-collapsed', 'false');
    const harness = await RouterTestingHarness.create();
    const layout = await harness.navigateByUrl('/dashboard', UserLayout);
    const scroll = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    const content = harness.routeNativeElement!.querySelector('.page-content') as HTMLElement;
    content.scrollTop = 120;
    expect(harness.routeNativeElement!.querySelector('.topbar h1')?.textContent).toBe('Kontrol Paneli');
    const sidebar = harness.routeNativeElement!.querySelector('.sidebar');
    (harness.routeNativeElement!.querySelector('.sidebar-toggle') as HTMLButtonElement).click();
    harness.detectChanges();
    expect(content.scrollTop).toBe(0);
    expect(scroll).toHaveBeenCalledWith({ top: 0, behavior: 'instant' });
    expect(sidebar?.hasAttribute('inert')).toBe(false);
    expect(sidebar?.getAttribute('aria-hidden')).not.toBe('true');
    expect(localStorage.getItem('dashboard-sidebar-collapsed')).toBe('true');
    for (const path of ['spatial-analysis', 'properties', 'property-map', 'dashboard']) {
      expect(await harness.navigateByUrl('/' + path, UserLayout)).toBe(layout);
      expect(harness.routeNativeElement!.querySelector('.sidebar')).toBe(sidebar);
      expect(layout.sidebarCollapsed()).toBe(true);
      expect(harness.routeNativeElement!.querySelector('.user-layout.sidebar-collapsed')).not.toBeNull();
      expect(harness.routeNativeElement!.querySelector('nav a.active')?.getAttribute('href')).toBe('/' + path);
    }
    await harness.navigateByUrl('/login', TestPage);
    expect(harness.routeNativeElement!.querySelector('.sidebar')).toBeNull();
    const restored = await harness.navigateByUrl('/properties', UserLayout);
    expect(restored.sidebarCollapsed()).toBe(true);
  });
});
