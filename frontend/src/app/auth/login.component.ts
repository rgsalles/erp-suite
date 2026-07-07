import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  loading = false;
  error = '';

  readonly form = this.fb.nonNullable.group({
    email: ['admin@erp.local', [Validators.required, Validators.email]],
    password: ['Admin@123', Validators.required]
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    const value = this.form.getRawValue();

    this.auth.login(value.email, value.password).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: () => {
        this.error = 'Email ou senha invalidos.';
        this.loading = false;
      }
    });
  }
}
