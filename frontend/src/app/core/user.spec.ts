import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { UserManagement } from './user';

describe('User', () => {
  let service: UserManagement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UserManagement);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
