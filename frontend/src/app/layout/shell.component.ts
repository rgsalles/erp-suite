import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../core/auth.service';
import { Language, LanguageService } from '../core/language.service';
import { OrganizationContextService } from '../core/organization-context.service';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslatePipe],
  templateUrl: './shell.component.html'
})
export class ShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly language = inject(LanguageService);
  readonly organization = inject(OrganizationContextService);

  readonly navItems = [
    { labelKey: 'nav.dashboard', path: '/dashboard' },
    { labelKey: 'nav.materials', path: '/materials' },
    { labelKey: 'nav.organization', path: '/organization' },
    { labelKey: 'nav.catalog', path: '/catalog' },
    { labelKey: 'nav.inventory', path: '/inventory' },
    { labelKey: 'nav.partners', path: '/partners' },
    { labelKey: 'nav.purchasing', path: '/purchasing' },
    { labelKey: 'nav.sales', path: '/sales' },
    { labelKey: 'nav.finance', path: '/finance' },
    { labelKey: 'nav.users', path: '/users' },
    { labelKey: 'nav.audit', path: '/audit-logs' }
  ];

  ngOnInit() {
    this.organization.load();
  }

  setLanguage(language: Language) {
    this.language.setLanguage(language);
  }
}
