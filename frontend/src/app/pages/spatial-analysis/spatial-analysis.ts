import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component, DestroyRef, ViewChild, inject, signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import {
  AnalysisMessage,
  SaveSpatialGeometriesRequest,
  SpatialAnalysisResponse,
  SpatialAnalysisService,
} from '../../core/spatial-analysis';
import {
  SpatialAnalysisMap,
} from '../../shared/spatial-analysis-map/spatial-analysis-map';

type AnalysisOperation = 'ab' | 'ba' | 'unionAB' | 'unionABC';

@Component({
  selector: 'app-spatial-analysis',
  imports: [RouterLink, DecimalPipe, SpatialAnalysisMap],
  templateUrl: './spatial-analysis.html',
  styleUrl: './spatial-analysis.scss',
})
export class SpatialAnalysis {
  @ViewChild(SpatialAnalysisMap)
  protected map!: SpatialAnalysisMap;

  private readonly service = inject(SpatialAnalysisService);
  private readonly destroyRef = inject(DestroyRef);

  readonly busy = signal(false);
  readonly message = signal('');
  readonly ready = signal(false);
  readonly geometries = signal<SpatialAnalysisResponse[]>([]);
  readonly result = signal<SpatialAnalysisResponse | null>(null);

  private draft: SaveSpatialGeometriesRequest | null = null;

  // Yeni çizim başladığında önceki ekran durumunu sıfırlar.
  startDrawing(): void {
    this.clear();
    this.map.startDrawing();
    this.message.set('A, B ve C poligonlarını sırayla çizin. Çift tıklayarak tamamlayın.');
  }

  onGeometriesChange(value: SaveSpatialGeometriesRequest | null): void {
    this.draft = value;
    this.ready.set(false);
    if (value) this.message.set('Üç poligon tamamlandı. Geometrileri kaydedin.');
  }

  save(): void {
    if (!this.draft) {
      this.message.set('Please complete geometries A, B, and C.');
      return;
    }

    this.run(this.service.save(this.draft), (items) => {
      this.acceptGeometries(items);
      this.message.set('A, B ve C kaydedildi.');
    });
  }

  loadSaved(): void {
    this.run(this.service.getSaved(), (items) => {
      this.map.showSaved(items);
      this.acceptGeometries(items);
      this.message.set('Kayıtlı A, B ve C geometrileri getirildi.');
    });
  }

  analyze(operation: AnalysisOperation): void {
    if (!this.ready()) return;

    const request: Observable<SpatialAnalysisResponse | AnalysisMessage> =
      operation === 'ab' || operation === 'ba'
        ? this.service.intersect(operation === 'ba')
        : this.service.union(operation === 'unionABC');

    this.result.set(null);
    this.map.clearResult();
    this.run(request, (response) => this.showAnalysis(response));
  }

  clear(): void {
    this.map.clear();
    this.draft = null;
    this.ready.set(false);
    this.geometries.set([]);
    this.result.set(null);
    this.message.set('');
  }

  private acceptGeometries(items: SpatialAnalysisResponse[]): void {
    this.geometries.set(items);
    this.result.set(null);
    this.map.clearResult();
    this.ready.set(true);
  }

  private showAnalysis(response: SpatialAnalysisResponse | AnalysisMessage): void {
    if ('message' in response) {
      this.message.set(response.message);
      return;
    }

    this.map.showResult(response);
    this.result.set(response);
    this.message.set('Analiz tamamlandı.');
  }

  // İsteklerin yükleme, hata ve abonelik temizliğini ortak yönetir.
  private run<T>(request: Observable<T>, success: (value: T) => void): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.message.set('');

    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.busy.set(false)),
    ).subscribe({
      next: success,
      error: (error: HttpErrorResponse) =>
        this.message.set(error.error?.message ?? 'İşlem tamamlanamadı.'),
    });
  }
}