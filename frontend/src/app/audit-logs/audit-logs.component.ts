import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ErpApiService } from '../core/erp-api.service';
import { LanguageService } from '../core/language.service';
import { AuditLog } from '../core/models';
import { TranslatePipe } from '../core/translate.pipe';

@Component({
  selector: 'app-audit-logs',
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './audit-logs.component.html'
})
export class AuditLogsComponent implements OnInit {
  private readonly api = inject(ErpApiService);
  private readonly language = inject(LanguageService);

  readonly logs = signal<AuditLog[]>([]);
  readonly error = signal('');

  entityName = '';
  take = 100;

  ngOnInit() {
    this.load();
  }

  load() {
    this.api.auditLogs({
      entityName: this.entityName || undefined,
      take: this.take
    }).subscribe({
      next: (logs) => {
        this.logs.set(logs);
        this.error.set('');
      },
      error: () => this.error.set(this.language.language() === 'en'
        ? 'Could not load logs. Only Admin and Manager can view audit logs.'
        : 'Nao foi possivel carregar os logs. Apenas Admin e Manager podem consultar auditoria.')
    });
  }

  formatDetails(details?: string | null) {
    if (!details) {
      return '';
    }

    try {
      return JSON.stringify(JSON.parse(details), null, 2);
    } catch {
      return details;
    }
  }
}
