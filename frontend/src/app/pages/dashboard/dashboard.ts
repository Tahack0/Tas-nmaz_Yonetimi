import { Component, DestroyRef, OnInit, inject , signal} from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Auth } from '../../core/auth';
import { Property, PropertyResponse, PropertyType } from '../../core/property';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(Auth);
  private readonly properties = inject(Property);
  private readonly destroyRef = inject(DestroyRef);

  readonly email = this.auth.getEmail();
  readonly isAdmin = this.auth.getRole()?.toLowerCase() === 'admin';

  // Signal kullanımı, asenkron sonuçlarda ekranı otomatik günceller.
  readonly loading = signal(true);
  readonly error = signal('');
  readonly recent = signal<PropertyResponse[]>([]);
  readonly stats = signal([
    { label: 'Toplam Taşınmaz', count: 0 },
    { label: 'Arsa', count: 0 },
    { label: 'Bina', count: 0 },
    { label: 'Konut', count: 0 },
  ]);
  // Menü ve hızlı erişim aynı bağlantı listesini kullanır.
  readonly links = [
    { label: 'Taşınmazlar', path: '/properties', icon: '▦', description: 'Kayıtları inceleyin ve işlemlerinizi yönetin.' },
    { label: 'Harita Görünümü', path: '/property-map', icon: '◎', description: 'Taşınmazları harita üzerinde görüntüleyin.' },
    ...(this.isAdmin ? [
      { label: 'Kullanıcı Yönetimi', path: '/users', icon: '♙', description: 'Kullanıcı hesaplarını ve rollerini yönetin.' },
      { label: 'Audit Kayıtları', path: '/audit-logs', icon: '≡', description: 'Sistemde yapılan işlemleri inceleyin.' },
    ] : []),
  ];

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.loading.set(true);
    this.error.set('');

    // Backend filtreleriyle toplamları getirir; bütün kayıtları indirmez.
    const countByType = (propertyType: PropertyType) =>
      this.properties.list({ page: 1, pageSize: 1, propertyType });

    forkJoin({
      recent: this.properties.list({ page: 1, pageSize: 5 }),
      land: countByType(PropertyType.Land),
      building: countByType(PropertyType.Building),
      residence: countByType(PropertyType.Residence),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ recent, land, building, residence }) => {
        const counts = [
          recent.totalCount, land.totalCount,
          building.totalCount, residence.totalCount,
        ];
        this.stats.update(items =>
          items.map((item, index) => ({ ...item, count: counts[index] })),
        );
        this.recent.set(recent.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kontrol paneli verileri yüklenemedi.');
        this.loading.set(false);
      },
    });
  }

}