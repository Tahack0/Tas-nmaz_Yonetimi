import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface PropertyPhotoResponse {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PropertyPhoto {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5008/api/properties';

  list(propertyId: string) {
    return this.http.get<PropertyPhotoResponse[]>(`${this.apiUrl}/${propertyId}/photos`);
  }

  upload(propertyId: string, file: File) {
    const data = new FormData();
    data.append('file', file, file.name);
    return this.http.post<PropertyPhotoResponse>(`${this.apiUrl}/${propertyId}/photos`, data);
  }

  // HttpClient kullanımı, fotoğraf isteğine JWT'nin de eklenmesini sağlar.
  getFile(propertyId: string, photoId: string) {
    return this.http.get(`${this.apiUrl}/${propertyId}/photos/${photoId}/file`, {
      responseType: 'blob',
    });
  }
}
