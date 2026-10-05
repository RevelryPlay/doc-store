import { DatePipe } from '@angular/common';
import { Component, ElementRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { DocumentActivity, DocumentDetails, UserSummary } from '../core/api.models';
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
  imports: [DatePipe],
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
  protected readonly activity = signal<DocumentActivity[]>([]);
  private readonly users = signal<UserSummary[]>([]);

  protected readonly otherUsers = computed(() =>
    this.users().filter((user) => user.id !== this.session.user()?.id),
  );
  protected readonly linkUrl = computed(() => {
    const token = this.document()?.shareLinkToken;
    return token ? `${location.origin}/s/${token}` : null;
  });

  open(documentId: string): void {
    forkJoin([this.api.get(documentId), this.api.users(), this.api.activity(documentId)]).subscribe(
      {
        next: ([document, users, activity]) => {
          this.document.set(document);
          this.users.set(users);
          this.activity.set(activity);
          this.dialog().nativeElement.showModal();
        },
        error: (error) =>
          this.announcer.announce(problemMessage(error, 'Could not load sharing settings.')),
      },
    );
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  protected describe(entry: DocumentActivity): string {
    switch (entry.action) {
      case 'Uploaded':
        return 'uploaded it';
      case 'Viewed':
        return 'viewed it';
      case 'Downloaded':
        return 'downloaded it';
      case 'Shared':
        return `shared it with ${entry.targetName}`;
      case 'Unshared':
        return `removed access for ${entry.targetName}`;
      case 'LinkEnabled':
        return 'turned on link sharing';
      case 'LinkDisabled':
        return 'turned off link sharing';
      case 'Deleted':
        return 'deleted it';
    }
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
        this.refreshActivity();
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

  private refreshActivity(): void {
    const id = this.document()?.id;
    if (id) {
      this.api.activity(id).subscribe((activity) => this.activity.set(activity));
    }
  }

  private updateDocument(change: (document: DocumentDetails) => DocumentDetails): void {
    this.document.update((document) => (document ? change(document) : document));
  }
}
