import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export enum PropertyType { Land = 1, Building = 2, Residence = 3 }

export interface PropertyResponse {
  id: string;
  city: string;
  district: string;
  neighborhood: string;
  parcelNumber: string;
  lotNumber: string;
  address: string;
  propertyType: PropertyType;
  geometryWkt: string;
  createdAt: string;
}

export interface PropertyRequest {
  city: string;
  district: string;
  neighborhood: string;
  parcelNumber: string;
  lotNumber: string;
  address: string;
  propertyType: PropertyType;
  exteriorRing: number[][];
}

export interface PropertyQuery {
  city?: string;
  district?: string;
  neighborhood?: string;
  parcelNumber?: string;
  lotNumber?: string;
  propertyType?: PropertyType;
  address?: string; // Adres filtresini API'ye gönderir.
  userId?: string; // Admin için taşınmaz sahibini filtreler.
  page: number;
  pageSize: number;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class Property {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5008/api/properties';

  list(query: PropertyQuery): Observable<PagedResponse<PropertyResponse>> {
    const params = this.createExportParameters(query)
      .set('page', query.page).set('pageSize', query.pageSize);
    return this.http.get<PagedResponse<PropertyResponse>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<PropertyResponse> {
    return this.http.get<PropertyResponse>(`${this.apiUrl}/${id}`);
  }

  create(request: PropertyRequest): Observable<PropertyResponse> {
    return this.http.post<PropertyResponse>(this.apiUrl, request);
  }

  update(id: string, request: PropertyRequest): Observable<PropertyResponse> {
    return this.http.put<PropertyResponse>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  exportExcel(query: PropertyQuery): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/excel`, {
      params: this.createExportParameters(query), responseType: 'blob',
    });
  }

  exportPdf(query: PropertyQuery): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/pdf`, {
      params: this.createExportParameters(query), responseType: 'blob',
    });
  }

  importExcel(file: File): Observable<unknown> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post(`${this.apiUrl}/import/excel`, formData);
  }

  // Listeleme ve indirmelerde aynı filtreleri kullanır; sayfalama eklemez.
  private createExportParameters(query: PropertyQuery): HttpParams {
    let parameters = new HttpParams();
    const keys = ['city', 'district', 'neighborhood', 'parcelNumber',
      'lotNumber', 'propertyType', 'address', 'userId'] as const;
    for (const key of keys) {
      const value = query[key];
      if (value !== undefined && value !== '') parameters = parameters.set(key, value);
    }
    return parameters;
  }
}
