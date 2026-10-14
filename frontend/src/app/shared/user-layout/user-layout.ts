import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { Auth } from '../../core/auth';
import { ScrollContentToTop } from './scroll-content-to-top';

@Component({
  selector: 'app-user-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, ScrollContentToTop],
  templateUrl: './user-layout.html',
  styleUrl: './user-layout.scss',
})
export class UserLayout {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly content = viewChild<ElementRef<HTMLElement>>('pageContent');
  private readonly currentUrl = toSignal(this.router.events.pipe(
    filter(event => event instanceof NavigationEnd),
    map(event => event.urlAfterRedirects),
  ), { initialValue: this.router.url });
  readonly pageTitle = computed(() => {
    const titles: Record<string, string> = {
      dashboard: 'Kontrol Paneli', properties: 'Taşınmazlar',
      'property-map': 'Taşınmaz Haritası', 'spatial-analysis': 'Alan Analizi',
      users: 'Kullanıcı Yönetimi', 'audit-logs': 'Audit Kayıtları',
    };
    return titles[this.currentUrl().split(/[/?#]/)[1]] ?? 'Kontrol Paneli';
  });

  readonly email = this.auth.getEmail();
  private readonly sidebarStorageKey = 'dashboard-sidebar-collapsed';
  readonly sidebarCollapsed = signal(this.readSidebarPreference());
  readonly avatarInitial = (this.email?.trim().charAt(0) || 'K').toLocaleUpperCase('tr-TR');

  private readSidebarPreference(): boolean {
    if (window.matchMedia?.('(max-width: 760px)').matches) return true;
    try {
      const saved = localStorage.getItem(this.sidebarStorageKey);
      return saved === 'true';
    } catch {
      return false;
    }
  }

  toggleSidebar(): void {
    window.scrollTo({ top: 0, behavior: 'instant' });
    const content = this.content()?.nativeElement;
    if (content) content.scrollTop = 0;
    this.sidebarCollapsed.update(collapsed => !collapsed);
    try {
      localStorage.setItem(this.sidebarStorageKey, String(this.sidebarCollapsed()));
    } catch {
      // Keep the menu usable when browser storage is unavailable.
    }
  }

  readonly isAdmin = this.auth.getRole()?.toLowerCase() === 'admin';

  readonly links = [
    { label: 'Taşınmazlar', path: '/properties', icon: '▦', description: 'Kayıtları inceleyin ve işlemlerinizi yönetin.' },
    { label: 'Harita Görünümü', path: '/property-map', icon: '◎', description: 'Taşınmazları harita üzerinde görüntüleyin.' },
    ...(this.isAdmin ? [
      { label: 'Kullanıcı Yönetimi', path: '/users', icon: '♙', description: 'Kullanıcı hesaplarını ve rollerini yönetin.' },
      { label: 'Audit Kayıtları', path: '/audit-logs', icon: '≡', description: 'Sistemde yapılan işlemleri inceleyin.' },
    ] : []),
  ];

  logout(): void {
    this.auth.logout().subscribe({
    next: () => { void this.router.navigate(['/login']); },
    error: () => window.alert('Çıkış yapılamadı. Sunucu bağlantısını kontrol edip tekrar deneyin.'),
   });
 }
}
