import { DatePipe } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { DocumentListItem } from '../core/api.models';
import { formatSize } from '../core/format';
import { DocumentsApi } from './documents-api';

@Component({
  selector: 'app-document-table',
  imports: [DatePipe],
  templateUrl: './document-table.html',
  styleUrl: './document-table.scss',
})
export class DocumentTable {
  private readonly api = inject(DocumentsApi);

  readonly documents = input.required<DocumentListItem[]>();
  readonly labelledBy = input.required<string>();
  readonly owned = input(false);
  readonly shareRequested = output<DocumentListItem>();
  readonly deleteRequested = output<DocumentListItem>();

  protected readonly previews = signal<Partial<Record<string, string>>>({});
  protected readonly formatSize = formatSize;

  protected contentUrl(document: DocumentListItem): string {
    return this.api.contentUrl(document.id);
  }

  protected sharingSummary(document: DocumentListItem): string {
    const parts = [];
    if (document.shareCount > 0) {
      parts.push(`${document.shareCount} ${document.shareCount === 1 ? 'person' : 'people'}`);
    }
    if (document.isLinkShared) {
      parts.push('link');
    }
    return parts.length > 0 ? parts.join(', ') : 'Private';
  }

  // Previews are fetched on first expand so the list query stays small.
  protected loadPreview(document: DocumentListItem, event: Event): void {
    const opened = (event.target as HTMLDetailsElement).open;
    if (!opened || document.id in this.previews()) {
      return;
    }

    this.api.get(document.id).subscribe((details) =>
      this.previews.update((previews) => ({
        ...previews,
        [document.id]: details.textPreview ?? 'No text could be extracted.',
      })),
    );
  }
}
