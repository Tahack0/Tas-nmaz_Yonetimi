import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export enum LogStatus {
  Successful = 1,
  Failed = 2,
}

export enum OperationType {
  Login = 1,
  Logout = 2,
  PropertyCreated = 3,
  PropertyUpdated = 4,
  PropertyDeleted = 5,
  PropertyImported = 6,
  PropertyExported = 7,
  UserCreated = 8,
  UserUpdated = 9,
  UserDeleted = 10,
  SpatialAnalysisPerformed = 11,
}

export interface AuditLogResponse {
  id: string;
  userId: string | null;
  status: LogStatus;
  operationType: OperationType;
  description: string;
  ipAddress: string;
  createdAt: string;
}

export interface AuditLogQuery {
  userId?: string;
  status?: LogStatus;
  operationType?: OperationType;
  description?: string;
  ipAddress?: string;
  fromUtc?: string;
  toUtc?: string;
  page: number;
  pageSize: number;
}

export interface AuditLogPagedResponse {
  items: AuditLogResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root',
})
export class AuditLog {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5008/api/admin/audit-logs';

  list(
    query: AuditLogQuery,
  ): Observable<AuditLogPagedResponse> {
    let parameters =
      this.createFilterParameters(query);

    parameters = parameters
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    return this.http.get<AuditLogPagedResponse>(
      this.apiUrl,
      {
        params: parameters,
      },
    );
  }

  exportExcel(
    query: AuditLogQuery,
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/export/excel`,
      {
        params: this.createFilterParameters(query),
        responseType: 'blob',
      },
    );
  }

  exportPdf(
    query: AuditLogQuery,
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/export/pdf`,
      {
        params: this.createFilterParameters(query),
        responseType: 'blob',
      },
    );
  }

  private createFilterParameters(
    query: AuditLogQuery,
  ): HttpParams {
    let parameters = new HttpParams();

    if (query.userId?.trim()) {
      parameters = parameters.set(
        'userId',
        query.userId.trim(),
      );
    }

    if (query.status) {
      parameters = parameters.set(
        'status',
        query.status,
      );
    }

    if (query.operationType) {
      parameters = parameters.set(
        'operationType',
        query.operationType,
      );
    }

    if (query.description?.trim()) {
      parameters = parameters.set(
        'description',
        query.description.trim(),
      );
    }

    if (query.ipAddress?.trim()) {
      parameters = parameters.set(
        'ipAddress',
        query.ipAddress.trim(),
      );
    }

    if (query.fromUtc) {
      parameters = parameters.set(
        'fromUtc',
        query.fromUtc,
      );
    }

    if (query.toUtc) {
      parameters = parameters.set(
        'toUtc',
        query.toUtc,
      );
    }

    return parameters;
  }
}