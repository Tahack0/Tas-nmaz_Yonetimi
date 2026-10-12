import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { Auth } from './auth';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  function intercept(token: string | null) {
    TestBed.configureTestingModule({
      providers: [{ provide: Auth, useValue: { getToken: () => token } }],
    });
    const request = new HttpRequest('GET', '/api/properties');
    let forwarded: HttpRequest<unknown> | undefined;
    TestBed.runInInjectionContext(() => authInterceptor(request, (value) => {
      forwarded = value;
      return of(new HttpResponse());
    }));
    return { request, forwarded: forwarded! };
  }
  it('adds the bearer token without modifying the original request', () => {
    const { request, forwarded } = intercept('test-token');
    expect(forwarded.headers.get('Authorization')).toBe('Bearer test-token');
    expect(request.headers.has('Authorization')).toBe(false);
  });
  it('forwards the original request when no token exists', () => {
    const { request, forwarded } = intercept(null);
    expect(forwarded).toBe(request);
    expect(forwarded.headers.has('Authorization')).toBe(false);
  });
});
