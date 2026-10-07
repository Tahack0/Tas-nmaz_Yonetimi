import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

// Seçim kutularında kullanılacak konum bilgisidir.
export interface LocationResponse {
  id: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5008/api/locations';

  getCities(): Observable<LocationResponse[]> {
    return this.http.get<LocationResponse[]>(
      `${this.apiUrl}/cities`,
    );
  }

  getDistricts(cityId: number): Observable<LocationResponse[]> {
    return this.http.get<LocationResponse[]>(
      `${this.apiUrl}/cities/${cityId}/districts`,
    );
  }

  getNeighborhoods(
    districtId: number,
  ): Observable<LocationResponse[]> {
    return this.http.get<LocationResponse[]>(
      `${this.apiUrl}/districts/${districtId}/neighborhoods`,
    );
  }
}