import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { Auth, RegisterRequest } from '../../core/auth';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly auth = inject(Auth);
  private readonly destroyRef = inject(DestroyRef);

  readonly saving = signal(false);
  readonly error = signal('');
  readonly completed = signal(false);

  form: RegisterRequest = { name: '', email: '', password: '' };
  passwordAgain = '';
  readonly passwordPattern = /^(?=.*\p{L})(?=.*\p{Nd})(?=.*[^\p{L}\p{Nd}\s])\S{8,12}$/u;

  register(registerForm: NgForm): void {
    if (this.saving()) return;
    this.error.set('');
    registerForm.control.markAllAsTouched();
    if (registerForm.invalid) return;

    if (this.form.password !== this.passwordAgain) {
      this.error.set('Parolalar eşleşmiyor.');
      return;
    }

    this.submitRegistration();
  }

  private submitRegistration(): void {
    this.saving.set(true);
    const request = { ...this.form, name: this.form.name.trim(), email: this.form.email.trim() };
    this.auth.register(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.saving.set(false)),
    ).subscribe({
      next: () => this.completeRegistration(),
      error: (error: HttpErrorResponse) => this.showError(error),
    });
  }

  private completeRegistration(): void {
    this.completed.set(true);
    this.form = { name: '', email: '', password: '' };
    this.passwordAgain = '';
  }

  private showError(error: HttpErrorResponse): void {
    const validationErrors = error.error?.errors;
    const messages = validationErrors
      ? Object.values(validationErrors).flat().join(' ')
      : '';

    this.error.set(
      error.error?.message || messages ||
      (error.status === 0 ? 'Sunucuya ulaşılamıyor.' : 'Kayıt oluşturulamadı.'),
    );
  }
}
