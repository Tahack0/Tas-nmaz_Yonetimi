import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import {
  UserManagement,
  UserPagedResponse,
  UserRequest,
  UserResponse,
  UserRole,
} from '../../core/user';

@Component({
  selector: 'app-users',
  imports: [FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class Users implements OnInit {
  private readonly userService =
    inject(UserManagement);

  private readonly changeDetector =
    inject(ChangeDetectorRef);

  protected readonly UserRole = UserRole;

  protected users: UserResponse[] = [];

  protected loading = false;
  protected saving = false;
  protected errorMessage = '';
  protected successMessage = '';

  protected showForm = false;
  protected editingId: string | null = null;

  protected page = 1;
  protected pageSize = 20;
  protected totalCount = 0;
  protected totalPages = 0;

  protected form: UserRequest =
    this.createEmptyForm();

  ngOnInit(): void {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService
      .list({
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (response: UserPagedResponse) => {
          this.users = response.items;
          this.page = response.page;
          this.pageSize = response.pageSize;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
          this.loading = false;

          this.changeDetector.detectChanges();
        },
        error: (error: HttpErrorResponse) => {
          this.loading = false;
          this.errorMessage =
            this.getErrorMessage(
              error,
              'Kullanıcılar yüklenirken hata oluştu.',
            );

          this.changeDetector.detectChanges();
        },
      });
  }

  protected startCreate(): void {
    this.editingId = null;
    this.form = this.createEmptyForm();
    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  protected startEdit(user: UserResponse): void {
    this.editingId = user.id;

    this.form = {
      name: user.name,
      email: user.email,
      password: '',
      role: user.role,
    };

    this.errorMessage = '';
    this.successMessage = '';
    this.showForm = true;
  }

  protected cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.form = this.createEmptyForm();
    this.errorMessage = '';
  }

  protected saveUser(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.form.name.trim()) {
      this.errorMessage =
        'Kullanıcı adı boş bırakılamaz.';
      return;
    }

    if (!this.form.email.trim()) {
      this.errorMessage =
        'E-posta adresi boş bırakılamaz.';
      return;
    }

    if (!this.isPasswordValid(this.form.password)) {
      this.errorMessage =
        'Parola 8-12 karakter olmalı; harf, rakam ve özel karakter içermelidir.';
      return;
    }

    const request: UserRequest = {
      name: this.form.name.trim(),
      email: this.form.email.trim(),
      password: this.form.password,
      role: this.form.role,
    };

    this.saving = true;

    const operation = this.editingId
      ? this.userService.update(
          this.editingId,
          request,
        )
      : this.userService.create(request);

    operation.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;

        this.successMessage = this.editingId
          ? 'Kullanıcı başarıyla güncellendi.'
          : 'Kullanıcı başarıyla oluşturuldu.';

        this.editingId = null;
        this.form = this.createEmptyForm();

        this.loadUsers();
      },
      error: (error: HttpErrorResponse) => {
        this.saving = false;
        this.errorMessage =
          this.getErrorMessage(
            error,
            'Kullanıcı kaydedilirken hata oluştu.',
          );

        this.changeDetector.detectChanges();
      },
    });
  }

  protected deleteUser(user: UserResponse): void {
    const approved = window.confirm(
      `${user.name} adlı kullanıcıyı silmek istediğinize emin misiniz?`,
    );

    if (!approved) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';

    this.userService.delete(user.id).subscribe({
      next: () => {
        this.successMessage =
          'Kullanıcı başarıyla silindi.';

        if (
          this.users.length === 1 &&
          this.page > 1
        ) {
          this.page--;
        }

        this.loadUsers();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          this.getErrorMessage(
            error,
            'Kullanıcı silinirken hata oluştu.',
          );

        this.changeDetector.detectChanges();
      },
    });
  }

  protected previousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.page--;
    this.loadUsers();
  }

  protected nextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.page++;
    this.loadUsers();
  }

  protected getRoleName(role: UserRole): string {
    return role === UserRole.Admin
      ? 'Admin'
      : 'Kullanıcı';
  }

  private createEmptyForm(): UserRequest {
    return {
      name: '',
      email: '',
      password: '',
      role: UserRole.User,
    };
  }

  private isPasswordValid(
    password: string,
  ): boolean {
    return /^(?=.*\p{L})(?=.*\d)(?=.*[^\p{L}\d\s])\S{8,12}$/u
      .test(password);
  }

  private getErrorMessage(
    error: HttpErrorResponse,
    fallbackMessage: string,
  ): string {
    if (typeof error.error?.message === 'string') {
      return error.error.message;
    }

    if (error.status === 403) {
      return 'Bu işlem için Admin yetkisi gerekiyor.';
    }

    if (error.status === 401) {
      return 'Oturum süresi dolmuş olabilir.';
    }

    return fallbackMessage;
  }
}