import { Component, ElementRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { DocumentDetails, UserSummary } from '../core/api.models';
import { Announcer } from '../core/announcer';
import { problemMessage } from '../core/problem';
import { Session } from '../core/session';
import { DocumentsApi } from './documents-api';

/**
 * Native modal dialog: the browser provides the focus trap, Escape to close,
 * and returns focus to the Share button that opened it.
 */
@Component({
  selector: 'app-share-dialog',
  templateUrl: './share-dialog.html',
  styleUrl: './share-dialog.scss',
})
export class ShareDialog {
  private readonly api = inject(DocumentsApi);
  private readonly announcer = inject(Announcer);
  private readonly session = inject(Session);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly changed = output<void>();

  protected readonly document = signal<DocumentDetails | null>(null);
  protected readonly busy = signal(false);
  private readonly users = signal<UserSummary[]>([]);

  protected readonly otherUsers = computed(() =>
    this.users().filter((user) => user.id !== this.session.user()?.id),
  );
  protected readonly linkUrl = computed(() => {
    const token = this.document()?.shareLinkToken;
    return token ? `${location.origin}/s/${token}` : null;
  });

  open(documentId: string): void {
    forkJoin([this.api.get(documentId), this.api.users()]).subscribe({
      next: ([document, users]) => {
        this.document.set(document);
        this.users.set(users);
        this.dialog().nativeElement.showModal();
      },
      error: (error) =>
        this.announcer.announce(problemMessage(error, 'Could not load sharing settings.')),
    });
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  protected isSharedWith(userId: string): boolean {
    return this.document()?.sharedWith.includes(userId) ?? false;
  }

  protected toggleUser(user: UserSummary, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const id = this.document()!.id;
    const grant = checkbox.checked;

    this.apply(
      checkbox,
      grant ? this.api.share(id, user.id) : this.api.unshare(id, user.id),
      () => {
        this.updateDocument((document) => ({
          ...document,
          sharedWith: grant
            ? [...document.sharedWith, user.id]
            : document.sharedWith.filter((userId) => userId !== user.id),
        }));
        this.announcer.announce(
          grant ? `Shared with ${user.displayName}.` : `Removed access for ${user.displayName}.`,
        );
      },
    );
  }

  protected toggleLink(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const id = this.document()!.id;

    if (checkbox.checked) {
      this.apply(checkbox, this.api.enableLink(id), (token) => {
        this.updateDocument((document) => ({ ...document, shareLinkToken: token }));
        this.announcer.announce('Link sharing turned on.');
      });
    } else {
      this.apply(checkbox, this.api.disableLink(id), () => {
        this.updateDocument((document) => ({ ...document, shareLinkToken: null }));
        this.announcer.announce('Link sharing turned off. The old link no longer works.');
      });
    }
  }

  protected copyLink(url: string): void {
    navigator.clipboard.writeText(url).then(
      () => this.announcer.announce('Link copied to clipboard.'),
      () => this.announcer.announce('Could not copy. Select the link text and copy it manually.'),
    );
  }

  // Controls stay enabled while a request is in flight because disabling the focused
  // checkbox would drop keyboard focus, so overlapping changes are ignored here instead.
  private apply<T>(
    checkbox: HTMLInputElement,
    request: Observable<T>,
    onSuccess: (value: T) => void,
  ): void {
    if (this.busy()) {
      checkbox.checked = !checkbox.checked;
      return;
    }

    this.busy.set(true);
    request.subscribe({
      next: (value) => {
        onSuccess(value);
        this.changed.emit();
      },
      error: (error) => {
        // The binding value never changed, so the DOM has to be put back by hand.
        checkbox.checked = !checkbox.checked;
        this.busy.set(false);
        this.announcer.announce(problemMessage(error, 'Sharing could not be updated.'));
      },
      complete: () => this.busy.set(false),
    });
  }

  private updateDocument(change: (document: DocumentDetails) => DocumentDetails): void {
    this.document.update((document) => (document ? change(document) : document));
  }
}
