import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  ActivatedRoute,
  Router,RouterLink
} from '@angular/router';
import { finalize } from 'rxjs';
import { Auth } from '../../core/auth';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,RouterLink
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  isLoading = false;
  errorMessage = '';

  readonly form = this.formBuilder.nonNullable.group({
    email: [
      '',
      [
        Validators.required,
        Validators.email,
      ],
    ],
    password: [
      '',
      Validators.required,
    ],
  });

  submit(): void {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.auth
      .login(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
      )
      .subscribe({
        next: () => {
          const returnUrl =
            this.route.snapshot.queryParamMap
              .get('returnUrl') ?? '/dashboard';

          void this.router.navigateByUrl(returnUrl);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage =
            error.error?.message ??
            'Giriş sırasında bir hata oluştu.';
        },
      });
  }
}