import {ChangeDetectorRef,Component,OnInit,inject,} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../core/auth';
import {
  PagedResponse,
  Property,
  PropertyQuery,
  PropertyRequest,
  PropertyResponse,
  PropertyType,
} from '../../core/property';


@Component({
  selector: 'app-properties',
  imports: [FormsModule],
  styleUrl: './properties.scss',
  templateUrl: './properties.html',
})
export class Properties implements OnInit {
  private readonly propertyService = inject(Property);
  private readonly auth = inject(Auth);
  private readonly changeDetector =inject(ChangeDetectorRef);
  protected readonly PropertyType = PropertyType;

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

  protected readonly isUser =
    this.auth.getRole()?.toLowerCase() === 'user';

  protected query: PropertyQuery = {
    city: '',
    district: '',
    neighborhood: '',
    parcelNumber: '',
    lotNumber: '',
    propertyType: undefined,
    page: 1,
    pageSize: 20,
  };

  protected form: PropertyRequest = this.createEmptyForm();

  protected coordinatesText = '';

  protected totalCount = 0;
  protected totalPages = 0;

  ngOnInit(): void {
    this.loadProperties();
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
    this.editingId = null;
    this.form = this.createEmptyForm();
    this.coordinatesText = '';
    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  protected startEdit(property: PropertyResponse): void {
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
      this.convertWktToCoordinates(property.geometryWkt);

    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  protected cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.createEmptyForm();
    this.coordinatesText = '';
  }

  protected saveProperty(): void {
  this.errorMessage = '';
  this.successMessage = '';
  this.saving = false;
  this.changeDetector.detectChanges();

  let coordinates =
    this.parseCoordinates(this.coordinatesText);

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
    next: () => {
      this.successMessage = this.editingId
        ? 'Taşınmaz başarıyla güncellendi.'
        : 'Taşınmaz başarıyla eklendi.';

      this.saving = false;
      this.showForm = false;
      this.editingId = null;
      this.form = this.createEmptyForm();
      this.coordinatesText = '';
      this.loadProperties();
    },
    error: (error: HttpErrorResponse) => {
      this.errorMessage =
        error.error?.message ??
        'Taşınmaz kaydedilirken bir hata oluştu.';

      this.saving = false;
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
    this.query.page = 1;
    this.loadProperties();
  }

  protected clearFilters(): void {
    this.query = {
      city: '',
      district: '',
      neighborhood: '',
      parcelNumber: '',
      lotNumber: '',
      propertyType: undefined,
      page: 1,
      pageSize: 20,
    };

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
}