import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { UserRole, UserSummary } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-users',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly language = inject(LanguageService);

  readonly users = signal<UserSummary[]>([]);
  readonly roles: UserRole[] = ['Admin', 'Manager', 'Buyer', 'Seller', 'Stock', 'Operator'];
  readonly error = signal('');

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.users().subscribe({
      next: (users) => this.users.set(users),
      error: () => this.error.set(this.language.language() === 'en'
        ? 'Only administrators can manage users.'
        : 'Apenas administradores podem gerenciar usuarios.')
    });
  }

  save(user: UserSummary) {
    this.api.updateUser(user.id, {
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      isActive: user.isActive
    }).subscribe({
      next: () => this.load(),
      error: () => this.error.set(this.language.language() === 'en' ? 'Could not save the user.' : 'Nao foi possivel salvar o usuario.')
    });
  }
}
