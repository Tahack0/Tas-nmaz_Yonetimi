import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import {
  AuditLog,
  AuditLogPagedResponse,
  AuditLogQuery,
  AuditLogResponse,
  LogStatus,
  OperationType,
} from '../../core/audit-log';

@Component({
  selector: 'app-audit-logs',
  imports: [FormsModule, DatePipe],
  templateUrl: './audit-logs.html',
  styleUrl: './audit-logs.scss',
})
export class AuditLogs implements OnInit {
  private readonly auditLogService =
    inject(AuditLog);

  private readonly changeDetector =
    inject(ChangeDetectorRef);

  protected readonly LogStatus = LogStatus;
  protected readonly OperationType = OperationType;

  protected auditLogs: AuditLogResponse[] = [];

  protected loading = false;
  protected exporting = false;
  protected errorMessage = '';

  protected fromLocal = '';
  protected toLocal = '';

  protected query: AuditLogQuery = {
    userId: '',
    status: undefined,
    operationType: undefined,
    description: '',
    ipAddress: '',
    fromUtc: undefined,
    toUtc: undefined,
    page: 1,
    pageSize: 20,
  };

  protected totalCount = 0;
  protected totalPages = 0;

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  protected loadAuditLogs(): void {
    this.loading = true;
    this.errorMessage = '';

    this.auditLogService.list(this.query).subscribe({
      next: (
        response: AuditLogPagedResponse,
      ) => {
        this.auditLogs = response.items;
        this.query.page = response.page;
        this.query.pageSize = response.pageSize;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.loading = false;

        this.changeDetector.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        this.errorMessage =
          this.getErrorMessage(
            error,
            'Audit kayıtları yüklenirken hata oluştu.',
          );

        this.changeDetector.detectChanges();
      },
    });
  }

  protected applyFilters(): void {
    if (!this.updateUtcDates()) {
      return;
    }

    this.query.page = 1;
    this.loadAuditLogs();
  }

  protected clearFilters(): void {
    this.query = {
      userId: '',
      status: undefined,
      operationType: undefined,
      description: '',
      ipAddress: '',
      fromUtc: undefined,
      toUtc: undefined,
      page: 1,
      pageSize: 20,
    };

    this.fromLocal = '';
    this.toLocal = '';
    this.errorMessage = '';

    this.loadAuditLogs();
  }

  protected previousPage(): void {
    if (this.query.page <= 1) {
      return;
    }

    this.query.page--;
    this.loadAuditLogs();
  }

  protected nextPage(): void {
    if (this.query.page >= this.totalPages) {
      return;
    }

    this.query.page++;
    this.loadAuditLogs();
  }

  protected exportExcel(): void {
    this.exportFile('excel');
  }

  protected exportPdf(): void {
    this.exportFile('pdf');
  }

  protected getStatusName(
    status: LogStatus,
  ): string {
    return status === LogStatus.Successful
      ? 'Başarılı'
      : 'Başarısız';
  }

  protected getOperationName(
    operationType: OperationType,
  ): string {
    switch (operationType) {
      case OperationType.Login:
        return 'Giriş';

      case OperationType.Logout:
        return 'Çıkış';

      case OperationType.PropertyCreated:
        return 'Taşınmaz Eklendi';

      case OperationType.PropertyUpdated:
        return 'Taşınmaz Güncellendi';

      case OperationType.PropertyDeleted:
        return 'Taşınmaz Silindi';

      case OperationType.PropertyImported:
        return 'Taşınmaz İçe Aktarıldı';

      case OperationType.PropertyExported:
        return 'Taşınmaz Dışa Aktarıldı';

      case OperationType.UserCreated:
        return 'Kullanıcı Oluşturuldu';

      case OperationType.UserUpdated:
        return 'Kullanıcı Güncellendi';

      case OperationType.UserDeleted:
        return 'Kullanıcı Silindi';

      case OperationType.SpatialAnalysisPerformed:
        return 'Mekânsal Analiz Yapıldı';

      default:
        return 'Bilinmiyor';
    }
  }

  private exportFile(
    type: 'excel' | 'pdf',
  ): void {
    if (!this.updateUtcDates()) {
      return;
    }

    this.exporting = true;
    this.errorMessage = '';

    const operation =
      type === 'excel'
        ? this.auditLogService.exportExcel(
            this.query,
          )
        : this.auditLogService.exportPdf(
            this.query,
          );

    operation.subscribe({
      next: (file: Blob) => {
        const extension =
          type === 'excel' ? 'xlsx' : 'pdf';

        this.downloadFile(
          file,
          `audit-kayitlari.${extension}`,
        );

        this.exporting = false;
        this.changeDetector.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.exporting = false;
        this.errorMessage =
          this.getErrorMessage(
            error,
            'Dosya dışa aktarılırken hata oluştu.',
          );

        this.changeDetector.detectChanges();
      },
    });
  }

  private updateUtcDates(): boolean {
    this.errorMessage = '';

    const fromDate = this.fromLocal
      ? new Date(this.fromLocal)
      : null;

    const toDate = this.toLocal
      ? new Date(this.toLocal)
      : null;

    if (
      (fromDate &&
        Number.isNaN(fromDate.getTime())) ||
      (toDate &&
        Number.isNaN(toDate.getTime()))
    ) {
      this.errorMessage =
        'Geçerli bir tarih aralığı girin.';
      return false;
    }

    if (
      fromDate &&
      toDate &&
      fromDate > toDate
    ) {
      this.errorMessage =
        'Başlangıç tarihi bitiş tarihinden sonra olamaz.';
      return false;
    }

    this.query.fromUtc =
      fromDate?.toISOString();

    this.query.toUtc =
      toDate?.toISOString();

    return true;
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

  private getErrorMessage(
    error: HttpErrorResponse,
    fallbackMessage: string,
  ): string {
    if (typeof error.error?.message === 'string') {
      return error.error.message;
    }

    if (error.status === 403) {
      return 'Bu ekran için Admin yetkisi gerekiyor.';
    }

    if (error.status === 401) {
      return 'Oturum süresi dolmuş olabilir.';
    }

    return fallbackMessage;
  }
}