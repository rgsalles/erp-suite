import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthResponse, RegisterRequest, UserSummary } from './models';

const tokenKey = 'erp-suite-token';
const userKey = 'erp-suite-user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  readonly token = signal<string | null>(localStorage.getItem(tokenKey));
  readonly user = signal<UserSummary | null>(this.loadStoredUser());
  readonly isAuthenticated = computed(() => !!this.token() && !!this.user());

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, { email, password }).pipe(
      tap((response) => this.persist(response))
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, request).pipe(
      tap((response) => this.persist(response))
    );
  }

  logout() {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    this.token.set(null);
    this.user.set(null);
    this.router.navigateByUrl('/login');
  }

  get tokenValue() {
    return this.token();
  }

  private persist(response: AuthResponse) {
    localStorage.setItem(tokenKey, response.token);
    localStorage.setItem(userKey, JSON.stringify(response.user));
    this.token.set(response.token);
    this.user.set(response.user);
  }

  private loadStoredUser(): UserSummary | null {
    const raw = localStorage.getItem(userKey);
    return raw ? JSON.parse(raw) as UserSummary : null;
  }
}
