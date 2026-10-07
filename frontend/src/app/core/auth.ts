import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5008/api/auth';
  private readonly tokenKey = 'access_token';
  private readonly expirationKey = 'access_token_expiration';

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, request)
      .pipe(
        tap((response) => {
          localStorage.setItem(
            this.tokenKey,  
            response.accessToken,
          );

          localStorage.setItem(
            this.expirationKey,
            response.expiresAtUtc,
          );
        }),
      );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const expiration =
      localStorage.getItem(this.expirationKey);

    if (!token || !expiration) {
      return false;
    }

    const expirationDate = new Date(expiration);

    if (expirationDate <= new Date()) {
      this.logout();
      return false;
    }

    return true;
  }
    getRole(): string | null {
    const payload = this.getTokenPayload();

    return (
        payload?.['role'] ??
        payload?.[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ] ??
        null
    );
    }

    getEmail(): string | null {
    const payload = this.getTokenPayload();

    return (
        payload?.['email'] ??
        payload?.[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
        ] ??
        null
    );
    }

    private getTokenPayload():
    Record<string, string> | null {
    const token = this.getToken();

    if (!token) {
        return null;
    }

    try {
        const encodedPayload = token.split('.')[1]
        .replace(/-/g, '+')
        .replace(/_/g, '/');

        const paddedPayload = encodedPayload.padEnd(
        Math.ceil(encodedPayload.length / 4) * 4,
        '=',
        );

        const bytes = Uint8Array.from(
        atob(paddedPayload),
        (character) => character.charCodeAt(0),
        );

        return JSON.parse(
        new TextDecoder().decode(bytes),
        );
    } catch {
        return null;
    }
    }
    register(request: RegisterRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.apiUrl}/register`,
      request,
    );
  }


  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.expirationKey);
  }
}