import { Component, inject } from '@angular/core';
import { Auth } from '../../core/auth';
import {Router,RouterLink,RouterLinkActive,} from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink,RouterLinkActive],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly role = this.auth.getRole();
  readonly email = this.auth.getEmail();
  readonly isAdmin = this.role === 'Admin';

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}