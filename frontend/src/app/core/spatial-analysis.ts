import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export enum SpatialGeometryLabel {
  A = 1,
  B = 2,
  C = 3,
  D = 4,
  E = 5,
}

export interface SaveSpatialGeometriesRequest {
  a: number[][];
  b: number[][];
  c: number[][];
}

export interface SpatialAnalysisResponse {
  label: SpatialGeometryLabel;
  geometryWkt: string;
  areaSquareMeters: number;
}

export interface AnalysisMessage {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SpatialAnalysisService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =
    'http://localhost:5008/api/spatial-analyses';

  // Manuel çizilen A/B/C geometrilerini kaydeder.
  save(
    request: SaveSpatialGeometriesRequest,
  ): Observable<SpatialAnalysisResponse[]> {
    return this.http.post<SpatialAnalysisResponse[]>(
      `${this.apiUrl}/geometries`,
      request,
    );
  }

  // Auto-Select için kayıtlı geometrileri getirir.
  getSaved(): Observable<SpatialAnalysisResponse[]> {
    return this.http.get<SpatialAnalysisResponse[]>(
      `${this.apiUrl}/geometries`,
    );
  }

  // Kesişim yoksa backend bir mesaj döndürür.
  intersect(
    reverseOrder = false,
  ): Observable<SpatialAnalysisResponse | AnalysisMessage> {
    return this.http.get<
      SpatialAnalysisResponse | AnalysisMessage
    >(`${this.apiUrl}/intersection`, {
      params: { reverseOrder },
    });
  }

  // A∪B veya A∪B∪C birleşimini hesaplayıp kaydeder.
  union(
    includeC = false,
  ): Observable<SpatialAnalysisResponse> {
    return this.http.post<SpatialAnalysisResponse>(
      `${this.apiUrl}/union`,
      null,
      { params: { includeC } },
    );
  }
}