import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export enum UserRole {
  User = 1,
  Admin = 2,
}

export interface UserResponse {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export interface UserRequest {
  name: string;
  email: string;
  password: string;
  role: UserRole;
}

export interface UserQuery {
  page: number;
  pageSize: number;
}

export interface UserPagedResponse {
  items: UserResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root',
})
export class UserManagement {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5008/api/users';

  list(
    query: UserQuery,
  ): Observable<UserPagedResponse> {
    const parameters = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    return this.http.get<UserPagedResponse>(
      this.apiUrl,
      {
        params: parameters,
      },
    );
  }

  getById(id: string): Observable<UserResponse> {
    return this.http.get<UserResponse>(
      `${this.apiUrl}/${id}`,
    );
  }

  create(
    request: UserRequest,
  ): Observable<UserResponse> {
    return this.http.post<UserResponse>(
      this.apiUrl,
      request,
    );
  }

  update(
    id: string,
    request: UserRequest,
  ): Observable<UserResponse> {
    return this.http.put<UserResponse>(
      `${this.apiUrl}/${id}`,
      request,
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`,
    );
  }
}