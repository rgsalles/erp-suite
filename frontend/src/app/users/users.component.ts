import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import { UserRole, UserSummary } from '../core/models';

@Component({
  selector: 'app-users',
  imports: [FormsModule],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  private readonly api = inject(ErpApiService);

  readonly users = signal<UserSummary[]>([]);
  readonly roles: UserRole[] = ['Admin', 'Manager', 'Buyer', 'Seller', 'Stock', 'Operator'];
  readonly error = signal('');

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.users().subscribe({
      next: (users) => this.users.set(users),
      error: () => this.error.set('Apenas administradores podem gerenciar usuarios.')
    });
  }

  save(user: UserSummary) {
    this.api.updateUser(user.id, {
      fullName: user.fullName,
      role: user.role,
      isActive: user.isActive
    }).subscribe({
      next: () => this.load(),
      error: () => this.error.set('Nao foi possivel salvar o usuario.')
    });
  }
}
