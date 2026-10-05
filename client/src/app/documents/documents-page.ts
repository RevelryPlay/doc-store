import { Component, DestroyRef, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DocumentLibrary, DocumentListItem } from '../core/api.models';
import { Announcer } from '../core/announcer';
import { problemMessage } from '../core/problem';
import { DocumentTable } from './document-table';
import { DocumentsApi } from './documents-api';
import { ShareDialog } from './share-dialog';

@Component({
  selector: 'app-documents-page',
  imports: [DocumentTable, ShareDialog],
  templateUrl: './documents-page.html',
  styleUrl: './documents-page.scss',
})
export class DocumentsPage {
  private readonly api = inject(DocumentsApi);
  private readonly announcer = inject(Announcer);
  private readonly destroyRef = inject(DestroyRef);
  private readonly shareDialog = viewChild.required(ShareDialog);
  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  private readonly ownedHeading = viewChild.required<ElementRef<HTMLElement>>('ownedHeading');

  protected readonly library = signal<DocumentLibrary | null>(null);
  protected readonly uploading = signal(false);
  protected readonly uploadError = signal<string | null>(null);

  constructor() {
    this.reload();
  }

  protected reload(): void {
    this.api
      .library()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((library) => this.library.set(library));
  }

  protected upload(event: SubmitEvent): void {
    event.preventDefault();
    const input = this.fileInput().nativeElement;
    const file = input.files?.[0];
    if (!file) {
      this.uploadError.set('Choose a PDF file to upload.');
      input.focus();
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);
    this.api.upload(file).subscribe({
      next: (id) => {
        this.uploading.set(false);
        input.value = '';
        this.announcer.announce(`Uploaded ${file.name}. Processing…`);
        this.reload();
        this.trackProcessing(id);
      },
      error: (error) => {
        this.uploading.set(false);
        this.uploadError.set(problemMessage(error, 'Upload failed.'));
        input.focus();
      },
    });
  }

  protected share(document: DocumentListItem): void {
    this.shareDialog().open(document.id);
  }

  protected remove(document: DocumentListItem): void {
    if (!confirm(`Delete “${document.fileName}”? This cannot be undone.`)) {
      return;
    }

    this.api.delete(document.id).subscribe({
      next: () => {
        this.announcer.announce(`Deleted ${document.fileName}.`);
        // The focused Delete button is gone with its row, so give focus a stable place to land.
        this.ownedHeading().nativeElement.focus();
        this.reload();
      },
      error: (error) => this.announcer.announce(problemMessage(error, 'Delete failed.')),
    });
  }

  private trackProcessing(id: string): void {
    this.api
      .waitForProcessing(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((document) => {
        this.announcer.announce(
          document.status === 'Ready'
            ? `${document.fileName} is ready: ${document.pageCount} ${document.pageCount === 1 ? 'page' : 'pages'}.`
            : `${document.fileName} could not be processed.`,
        );
        this.reload();
      });
  }
}
