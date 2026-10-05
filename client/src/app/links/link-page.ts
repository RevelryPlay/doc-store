import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { AccessLevel, DocumentDetails } from '../core/api.models';
import { formatSize } from '../core/format';
import { DocumentsApi } from '../documents/documents-api';

@Component({
  selector: 'app-link-page',
  imports: [DatePipe],
  templateUrl: './link-page.html',
  styleUrl: './link-page.scss',
})
export class LinkPage implements OnInit {
  private readonly api = inject(DocumentsApi);

  readonly token = input.required<string>();

  protected readonly document = signal<DocumentDetails | null>(null);
  protected readonly unavailable = signal(false);
  protected readonly formatSize = formatSize;

  ngOnInit(): void {
    this.api.getByLink(this.token()).subscribe({
      next: (document) => this.document.set(document),
      error: () => this.unavailable.set(true),
    });
  }

  protected contentUrl(document: DocumentDetails): string {
    return this.api.contentUrl(document.id, this.token());
  }

  protected accessLabel(access: AccessLevel): string {
    switch (access) {
      case 'Owner':
        return 'You own this document';
      case 'SharedUser':
        return 'Shared with you';
      default:
        return 'Shared by link';
    }
  }
}
