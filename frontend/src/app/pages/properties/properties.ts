import {ChangeDetectorRef,Component,OnInit,inject,} from '@angular/core';
import { ReactiveFormsModule,FormBuilder,Validators,} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../core/auth';
import { UserManagement, UserResponse } from '../../core/user';
import {PagedResponse,Property,PropertyQuery,PropertyRequest,PropertyResponse,PropertyType,} from '../../core/property';
import { PropertyPhotos } from '../../shared/property-photos/property-photos';
import { PropertyMap } from '../../shared/property-map/property-map';
import { PropertyMapOverview } from '../property-map-overview/property-map-overview';
import {LocationService,LocationResponse,} from '../../core/location';
@Component({
  selector: 'app-properties',
  imports: [ReactiveFormsModule, PropertyMap , PropertyMapOverview, PropertyPhotos],
  styleUrl: './properties.scss',
  templateUrl: './properties.html',
})
export class Properties implements OnInit {
  private readonly propertyService = inject(Property);
  private readonly userService = inject(UserManagement);

  // Kullanıcı seçiminde ad ve e-posta gösterilir; ID arka planda kalır.
  protected filterUsers: UserResponse[] = [];
  protected usersLoading = false;
  protected usersError = '';

  // Kullanıcı API'si sayfalı olduğu için tüm sayfaları yükler.
  protected loadFilterUsers(page = 1, collected: UserResponse[] = []): void {
    if (!this.isAdmin || (page === 1 && this.usersLoading)) return;
    if (page === 1) {
      this.usersLoading = true;
      this.usersError = '';
    }
    this.userService.list({ page, pageSize: 100 }).subscribe({
      next: (response) => {
        const users = [...collected, ...response.items];
        if (page < response.totalPages) {
          this.loadFilterUsers(page + 1, users);
          return;
        }
        this.filterUsers = users.sort((a, b) => a.name.localeCompare(b.name, 'tr'));
        this.usersLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.usersLoading = false;
        this.usersError = 'Kullanıcı listesi yüklenemedi.';
        this.changeDetector.markForCheck();
      },
    });
  }

  private readonly auth = inject(Auth);
  private readonly changeDetector =inject(ChangeDetectorRef);
  protected readonly PropertyType = PropertyType;
  protected mapCoordinates: number[][] = [];
  protected viewMode: 'list' | 'map' = 'list';
  // Fotoğraf panelindeki taşınmazı seçer.
  protected photoPropertyId: string | null = null;
  protected properties: PropertyResponse[] = [];
  protected loading = false;
  protected saving = false;
  protected errorMessage = '';
  protected successMessage = '';
  protected selectedExcelFile: File | null = null;
  protected importing = false;
  protected exporting = false;
  protected showForm = false;
  protected editingId: string | null = null;
  // Kullanıcı filtresini yalnız Admin ekranında gösterir.
  protected readonly isAdmin = this.auth.getRole()?.toLowerCase() === 'admin';

  protected readonly isUser =
    this.auth.getRole()?.toLowerCase() === 'user';
  protected query: PropertyQuery = {
    city: '',
    district: '',
    neighborhood: '',
    parcelNumber: '',
    lotNumber: '',
    propertyType: undefined,
      address: '',
      userId: '',
    page: 1,
    pageSize: 20,
  };
  private readonly formBuilder = inject(FormBuilder);
  // Liste filtrelerini kayıt formundan ayrı tutar.
  protected readonly filterForm = this.formBuilder.nonNullable.group({
    city: ['', Validators.maxLength(100)],
    district: ['', Validators.maxLength(100)],
    neighborhood: ['', Validators.maxLength(150)],
    parcelNumber: ['', Validators.maxLength(50)],
    lotNumber: ['', Validators.maxLength(50)],
    address: ['', Validators.maxLength(500)],
    userId: ['', Validators.pattern(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)],
    propertyType: this.formBuilder.control<PropertyType | null>(null),
  });

  // Taşınmaz alanlarını ve doğrulama kurallarını tanımlar.
  protected readonly propertyForm =
    this.formBuilder.nonNullable.group({
      city: ['', [Validators.required, Validators.maxLength(100)]],
      district: ['', [Validators.required, Validators.maxLength(100)]],
      neighborhood: ['', [Validators.required, Validators.maxLength(150)]],
      parcelNumber: ['', [Validators.required, Validators.maxLength(50)]],
      lotNumber: ['', [Validators.required, Validators.maxLength(50)]],
      address: ['', [Validators.required, Validators.maxLength(500)]],
      propertyType: [PropertyType.Land, Validators.required],
      exteriorRing: this.formBuilder.nonNullable.control<number[][]>([]),
    });
  // Mevcut kayıt metotlarının form verisini okumasını sağlar.
  protected get form(): PropertyRequest {
    return this.propertyForm.getRawValue();
  }
  // Mevcut ekleme, düzenleme ve kapatma metotları formu sıfırlayabilir.
  protected set form(value: PropertyRequest) {
    this.propertyForm.reset(value);
  }
  private readonly locationService = inject(LocationService);
  // API’den gelen illeri tutar.
  protected cities: LocationResponse[] = [];
  protected citiesLoading = false;
  // Seçilen ilin ilçelerini tutar.
  protected districts: LocationResponse[] = [];
  protected districtsLoading = false;
  protected neighborhoods: LocationResponse[] = [];
  protected neighborhoodsLoading = false;
  // Önceki formdan veya seçimden kalan istekleri ayırt eder.
  private districtRequest = 0;
  private neighborhoodRequest = 0;
  // Eski kayıtlardaki İstanbul/Istanbul gibi yazım farklarını eşleştirir.
  private matchLocation(items: LocationResponse[], value: string): LocationResponse | undefined {
    const exact = items.find(item => item.name === value);
    if (exact || !value.trim()) return exact;
    const normalize = (name: string) => name.trim().toLocaleLowerCase('tr-TR')
      .replace(/ı/g, 'i').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
      .replace(/\s+/g, ' ');
    const matches = items.filter(item => normalize(item.name) === normalize(value));
    return matches.length === 1 ? matches[0] : undefined;
  }

  private loadCities(): void {
    this.citiesLoading = true;
    this.locationService.getCities().subscribe({
      next: (cities) => {
        this.cities = cities;
        this.citiesLoading = false;
        if (this.showForm) this.loadDistricts(true);
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.citiesLoading = false;
        this.errorMessage = 'İller yüklenemedi.';
        this.changeDetector.markForCheck();
      },
    });
  }
  // İl değişince ilçeleri yükler; düzenlemede mevcut seçimi korur.
  protected loadDistricts(keepSelection = false): void {
    const request = ++this.districtRequest;
    ++this.neighborhoodRequest;
    const controls = this.propertyForm.controls;
    const city = this.matchLocation(this.cities, controls.city.value);
    if (keepSelection && city) controls.city.setValue(city.name);
    this.districts = [];
    this.neighborhoods = [];
    this.districtsLoading = false;
    this.neighborhoodsLoading = false;
    if (!keepSelection) {
      controls.district.setValue('');
      controls.neighborhood.setValue('');
    }
    if (!city) {
      if (keepSelection && controls.city.value) {
        controls.city.setValue(''); controls.district.setValue(''); controls.neighborhood.setValue('');
        this.errorMessage = 'Eski konum listede bulunamadı. İl, ilçe ve mahalleyi yeniden seçin.';
      }
      return;
    }
    this.districtsLoading = true;
    this.locationService.getDistricts(city.id).subscribe({
      next: (districts) => {
        if (
          request !== this.districtRequest ||
          controls.city.value !== city.name
        ) return;
        this.districts = districts;
        this.districtsLoading = false;
        if (keepSelection) this.loadNeighborhoods(true);
        this.changeDetector.markForCheck();
      },
      error: () => {
        if (request !== this.districtRequest) return;
        this.districtsLoading = false;
        this.errorMessage = 'İlçeler yüklenemedi.';
        this.changeDetector.markForCheck();
      },
    });
  }
  // İlçe değişince bağlı mahalleleri yükler.
  protected loadNeighborhoods(keepSelection = false): void {
    const request = ++this.neighborhoodRequest;
    const controls = this.propertyForm.controls;
    const district = this.matchLocation(this.districts, controls.district.value);
    if (keepSelection && district) controls.district.setValue(district.name);
    this.neighborhoods = [];
    this.neighborhoodsLoading = false;
    if (!keepSelection) controls.neighborhood.setValue('');
    if (!district) {
      if (keepSelection && controls.district.value) {
        controls.district.setValue(''); controls.neighborhood.setValue('');
        this.errorMessage = 'Eski ilçe listede bulunamadı. İlçe ve mahalleyi yeniden seçin.';
      }
      return;
    }
    this.neighborhoodsLoading = true;
    this.locationService.getNeighborhoods(district.id).subscribe({
      next: (neighborhoods) => {
        if (
          request !== this.neighborhoodRequest ||
          controls.district.value !== district.name
        ) return;
        this.neighborhoods = neighborhoods;
        if (keepSelection && controls.neighborhood.value) {
          const selected = this.matchLocation(neighborhoods, controls.neighborhood.value);
          controls.neighborhood.setValue(selected?.name ?? '');
          if (!selected) this.errorMessage = 'Eski mahalle listede bulunamadı. Mahalleyi yeniden seçin.';
        }
        this.neighborhoodsLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        if (request !== this.neighborhoodRequest) return;
        this.neighborhoodsLoading = false;
        this.errorMessage = 'Mahalleler yüklenemedi.';
        this.changeDetector.markForCheck();
      },
    });
  }
  protected coordinatesText = '';
  protected totalCount = 0;
  protected totalPages = 0;
  protected changeView(
    viewMode: 'list' | 'map',
  ): void {
    this.viewMode = viewMode;
  }
  ngOnInit(): void {
    this.loadProperties();
    this.loadCities();
    if (this.isAdmin) this.loadFilterUsers();
  }
  protected loadProperties(): void {
  this.loading = true;
  this.errorMessage = '';
    this.propertyService.list(this.query).subscribe({
      next: (
        response: PagedResponse<PropertyResponse>,
      ) => {
        this.properties = response.items;
        this.query.page = response.page;
        this.query.pageSize = response.pageSize;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.loading = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.errorMessage =
          'Taşınmazlar yüklenirken bir hata oluştu.';
        this.loading = false;
        this.changeDetector.detectChanges();
      },
    });
  }
  protected startCreate(): void {
    this.photoPropertyId = null;
    this.editingId = null;
  this.form = this.createEmptyForm();
  this.coordinatesText = '';
  this.mapCoordinates = [];
  this.errorMessage = '';
  this.successMessage = '';
  this.showForm = true;
  this.loadDistricts(true);
  }
  protected startEdit(property: PropertyResponse): void {
    this.photoPropertyId = null;
    this.editingId = property.id;
    this.form = {
      city: property.city,
      district: property.district,
      neighborhood: property.neighborhood,
      parcelNumber: property.parcelNumber,
      lotNumber: property.lotNumber,
      address: property.address,
      propertyType: property.propertyType,
      exteriorRing: [],
    };
    this.coordinatesText =
      this.convertWktToCoordinates(
        property.geometryWkt,
      );
      this.mapCoordinates =
        this.parseCoordinates(
          this.coordinatesText,
        );
    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
    this.loadDistricts(true);
  }
  protected cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.createEmptyForm();
    this.coordinatesText = '';
    this.mapCoordinates = [];
    ++this.districtRequest;
    ++this.neighborhoodRequest;
  }
  protected saveProperty(): void {
    // Geçersiz formun API’ye gönderilmesini engeller.
    if (this.saving) return;
    // Konum listeleri tamamlanmadan kayıt yapılmasını engeller.
    if (this.citiesLoading || this.districtsLoading || this.neighborhoodsLoading) return;
    if (this.propertyForm.invalid) {
      this.propertyForm.markAllAsTouched();
      this.errorMessage = 'Zorunlu alanları kontrol edin.';
      return;
  }
  this.errorMessage = '';
  this.successMessage = '';
  this.saving = false;
  this.changeDetector.detectChanges();
  let coordinates =
    this.mapCoordinates.length > 0
      ? this.mapCoordinates.map(
          (coordinate) => [...coordinate],
        )
      : this.parseCoordinates(
          this.coordinatesText,
        );
  if (
    coordinates.length > 1 &&
    coordinates[0][0] ===
      coordinates[coordinates.length - 1][0] &&
    coordinates[0][1] ===
      coordinates[coordinates.length - 1][1]
  ) {
    coordinates = coordinates.slice(0, -1);
  }
  if (coordinates.length !== 4) {
    this.errorMessage =
      'Poligon için tam olarak dört farklı nokta girilmelidir.';
    return;
  }
  const closedExteriorRing: number[][] = [
    ...coordinates,
    [...coordinates[0]],
  ];
  const request: PropertyRequest = {
    ...this.form,
    exteriorRing: closedExteriorRing,
  };
  this.saving = true;
  const operation = this.editingId
    ? this.propertyService.update(
        this.editingId,
        request,
      )
    : this.propertyService.create(request);
  operation.subscribe({
    next: (savedProperty) => {
      this.successMessage = this.editingId
        ? 'Taşınmaz başarıyla güncellendi.'
        : 'Taşınmaz başarıyla eklendi.';
      // Kayıt tamamlanınca isteğe bağlı fotoğraf panelini açar.
      this.photoPropertyId = savedProperty.id;
      this.saving = false;
      this.showForm = false;
      this.editingId = null;
      this.form = this.createEmptyForm();
      this.coordinatesText = '';
      this.mapCoordinates = [];
      this.loadProperties();
    },
    error: (error: HttpErrorResponse) => {
      this.errorMessage =
        error.error?.message ??
        'Taşınmaz kaydedilirken bir hata oluştu.';
      this.saving = false;
      this.changeDetector.markForCheck();
    },
  });
}
  protected deleteProperty(
    property: PropertyResponse,
  ): void {
    const approved = window.confirm(
      `${property.city} / ${property.district} taşınmazını silmek istediğinize emin misiniz?`,
    );
    if (!approved) {
      return;
    }
    this.errorMessage = '';
    this.successMessage = '';
    this.propertyService.delete(property.id).subscribe({
      next: () => {
        this.successMessage =
          'Taşınmaz başarıyla silindi.';
        if (
          this.properties.length === 1 &&
          this.query.page > 1
        ) {
          this.query.page--;
        }
        if (this.photoPropertyId === property.id) this.photoPropertyId = null;
        this.loadProperties();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          error.error?.message ??
          'Taşınmaz silinirken bir hata oluştu.';
        this.changeDetector.detectChanges();
      },
    });
  }
  protected applyFilters(): void {
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      this.errorMessage = 'Filtre alanlarını kontrol edin.';
      return;
    }
    const values = this.filterForm.getRawValue();
    // Yeni filtre uygulandığında ilk sayfaya döner.
    this.query = {
      ...this.query,
      city: values.city.trim(), district: values.district.trim(),
      neighborhood: values.neighborhood.trim(), parcelNumber: values.parcelNumber.trim(),
      lotNumber: values.lotNumber.trim(), address: values.address.trim(),
      userId: this.isAdmin ? values.userId.trim() : undefined,
      propertyType: values.propertyType ?? undefined, page: 1,
    };
    this.loadProperties();
  }

  protected clearFilters(): void {
    // Ekrandaki filtreleri ve API sorgusunu birlikte temizler.
    this.filterForm.reset();
    this.query = { page: 1, pageSize: this.query.pageSize };
    this.loadProperties();
  }

  protected previousPage(): void {
    if (this.query.page <= 1) {
      return;
    }
    this.query.page--;
    this.loadProperties();
  }
  protected nextPage(): void {
    if (this.query.page >= this.totalPages) {
      return;
    }
    this.query.page++;
    this.loadProperties();
  }
  protected getPropertyTypeName(
    propertyType: PropertyType,
  ): string {
    switch (propertyType) {
      case PropertyType.Land:
        return 'Arsa';
      case PropertyType.Building:
        return 'Bina';
      case PropertyType.Residence:
        return 'Konut';
      default:
        return 'Bilinmiyor';
    }
  }
  private createEmptyForm(): PropertyRequest {
    return {
      city: '',
      district: '',
      neighborhood: '',
      parcelNumber: '',
      lotNumber: '',
      address: '',
      propertyType: PropertyType.Land,
      exteriorRing: [],
    };
  }
  private parseCoordinates(value: string): number[][] {
    return value
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line.length > 0)
      .map((line) =>
        line.split(',').map((coordinate) =>
          Number(coordinate.trim()),
        ),
      )
      .filter(
        (coordinate) =>
          coordinate.length === 2 &&
          coordinate.every(Number.isFinite),
      );
  }
  private convertWktToCoordinates(wkt: string): string {
    const coordinates = wkt
      .replace(/^POLYGON\s*\(\(/i, '')
      .replace(/\)\)\s*$/i, '')
      .split(',')
      .map((coordinate) => coordinate.trim())
      .map((coordinate) => coordinate.split(/\s+/));
    if (
      coordinates.length > 1 &&
      coordinates[0][0] ===
        coordinates[coordinates.length - 1][0] &&
      coordinates[0][1] ===
        coordinates[coordinates.length - 1][1]
    ) {
      coordinates.pop();
    }
    return coordinates
      .map((coordinate) => coordinate.join(', '))
      .join('\n');
  }
    protected selectExcelFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedExcelFile =
      input.files?.item(0) ?? null;
    this.errorMessage = '';
    this.successMessage = '';
  }
  protected importExcel(): void {
    if (!this.selectedExcelFile) {
      this.errorMessage =
        'Lütfen bir Excel dosyası seçin.';
      return;
    }
    if (
      !this.selectedExcelFile.name
        .toLowerCase()
        .endsWith('.xlsx')
    ) {
      this.errorMessage =
        'Yalnızca .xlsx uzantılı dosyalar yüklenebilir.';
      return;
    }
    this.importing = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.propertyService
      .importExcel(this.selectedExcelFile)
      .subscribe({
        next: () => {
          this.importing = false;
          this.selectedExcelFile = null;
          this.successMessage =
            'Excel dosyası başarıyla içe aktarıldı.';
          this.loadProperties();
        },
        error: (error: HttpErrorResponse) => {
          this.importing = false;
          this.errorMessage =
            error.error?.message ??
            'Excel dosyası içe aktarılırken hata oluştu.';
          this.changeDetector.detectChanges();
        },
      });
  }
  protected exportExcel(): void {
    this.exportFile('excel');
  }
  protected exportPdf(): void {
    this.exportFile('pdf');
  }
  private exportFile(type: 'excel' | 'pdf'): void {
    this.exporting = true;
    this.errorMessage = '';
    const operation =
      type === 'excel'
        ? this.propertyService.exportExcel(this.query)
        : this.propertyService.exportPdf(this.query);
    operation.subscribe({
      next: (file: Blob) => {
        const extension =
          type === 'excel' ? 'xlsx' : 'pdf';
        this.downloadFile(
          file,
          `tasinmazlar.${extension}`,
        );
        this.exporting = false;
        this.changeDetector.detectChanges();
      },
      error: () => {
        this.exporting = false;
        this.errorMessage =
          'Dosya dışa aktarılırken bir hata oluştu.';
        this.changeDetector.detectChanges();
      },
    });
  }
  private downloadFile(
    file: Blob,
    fileName: string,
  ): void {
    const url = URL.createObjectURL(file);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
  protected onMapCoordinatesChange(
    coordinates: number[][],
  ): void {
    this.mapCoordinates = coordinates;
    this.coordinatesText = coordinates
      .map((coordinate) =>
        `${coordinate[0]}, ${coordinate[1]}`,
      )
      .join('\n');
    this.errorMessage = '';
  }
}