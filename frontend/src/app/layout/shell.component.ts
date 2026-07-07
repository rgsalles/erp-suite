import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html'
})
export class ShellComponent {
  readonly auth = inject(AuthService);

  readonly navItems = [
    { label: 'Dashboard', path: '/dashboard' },
    { label: 'Materiais', path: '/materials' },
    { label: 'Estoque', path: '/inventory' },
    { label: 'Parceiros', path: '/partners' },
    { label: 'Compras', path: '/purchasing' },
    { label: 'Vendas', path: '/sales' },
    { label: 'Financeiro', path: '/finance' },
    { label: 'Usuarios', path: '/users' },
    { label: 'Auditoria', path: '/audit-logs' }
  ];
}
