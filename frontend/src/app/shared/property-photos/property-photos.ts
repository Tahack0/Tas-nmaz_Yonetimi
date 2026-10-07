import { Component, DestroyRef, EventEmitter, Input, OnChanges, OnDestroy, Output, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PropertyPhoto, PropertyPhotoResponse } from '../../core/property-photo';

@Component({
  selector: 'app-property-photos',
  templateUrl: './property-photos.html',
  styleUrl: './property-photos.scss',
})
export class PropertyPhotos implements OnChanges, OnDestroy {
  @Input() propertyId = '';
  @Input() canUpload = false;
  @Output() readonly closed = new EventEmitter<void>();
  private readonly service = inject(PropertyPhoto);
  private readonly destroyRef = inject(DestroyRef);
  private generation = 0;
  private imageRequest = 0;
  private selectedFile: File | null = null;

  protected readonly photos = signal<PropertyPhotoResponse[]>([]);
  protected readonly loading = signal(false);
  protected readonly uploading = signal(false);
  protected readonly imageLoading = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');
  protected readonly fileName = signal('');
  protected readonly imageUrl = signal('');
  protected readonly imageName = signal('');

  ngOnChanges(): void {
    ++this.generation;
    this.releaseImage();
    this.selectedFile = null;
    this.fileName.set('');
    this.error.set('');
    this.success.set('');
    this.photos.set([]);
    if (this.propertyId) this.loadPhotos();
  }

  ngOnDestroy(): void { this.releaseImage(); }

  protected loadPhotos(): void {
    const generation = this.generation;
    this.loading.set(true);
    this.service.list(this.propertyId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: photos => {
        if (generation !== this.generation) return;
        this.photos.set(photos);
        this.loading.set(false);
      },
      error: () => {
        if (generation !== this.generation) return;
        this.loading.set(false);
        this.error.set('Fotoğraflar yüklenemedi.');
      },
    });
  }

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0) ?? null;
    this.selectedFile = null;
    this.fileName.set('');
    this.error.set('');
    this.success.set('');
    if (!file) return;
    // Sunucudaki JPEG/PNG ve 100 MB sınırını ekranda da kontrol eder.
    if (!/\.(jpe?g|png)$/i.test(file.name) || file.size === 0 || file.size > 100 * 1024 * 1024) {
      this.error.set('Boş olmayan JPEG/PNG dosyası seçin. En fazla 100 MB olabilir.');
      input.value = '';
      return;
    }
    this.selectedFile = file;
    this.fileName.set(file.name);
  }

  protected upload(input: HTMLInputElement): void {
    if (!this.canUpload || !this.selectedFile || this.uploading()) return;
    const generation = this.generation;
    this.uploading.set(true);
    this.error.set('');
    this.success.set('');
    this.service.upload(this.propertyId, this.selectedFile)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: photo => {
          if (generation !== this.generation) return;
          this.photos.update(photos => [photo, ...photos]);
          this.uploading.set(false);
          this.selectedFile = null;
          this.fileName.set('');
          input.value = '';
          this.success.set('Fotoğraf yüklendi.');
          this.showPhoto(photo);
        },
        error: (error: HttpErrorResponse) => {
          if (generation !== this.generation) return;
          this.uploading.set(false);
          this.error.set(error.error?.message ?? 'Fotoğraf yüklenemedi.');
        },
      });
  }

  protected showPhoto(photo: PropertyPhotoResponse): void {
    const generation = this.generation;
    const request = ++this.imageRequest;
    this.imageLoading.set(true);
    this.error.set('');
    // Fotoğrafları seçildikçe yükler; bütün büyük dosyaları aynı anda indirmez.
    this.service.getFile(this.propertyId, photo.id)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          if (generation !== this.generation || request !== this.imageRequest) return;
          this.releaseImage(false);
          this.imageUrl.set(URL.createObjectURL(blob));
          this.imageName.set(photo.fileName);
          this.imageLoading.set(false);
        },
        error: () => {
          if (generation !== this.generation || request !== this.imageRequest) return;
          this.imageLoading.set(false);
          this.error.set('Fotoğraf görüntülenemedi.');
        },
      });
  }

  private releaseImage(invalidate = true): void {
    // Panel kapandığında veya fotoğraf değiştiğinde belleği serbest bırakır.
    if (invalidate) ++this.imageRequest;
    if (this.imageUrl()) URL.revokeObjectURL(this.imageUrl());
    this.imageUrl.set('');
    this.imageName.set('');
    this.imageLoading.set(false);
  }
}
