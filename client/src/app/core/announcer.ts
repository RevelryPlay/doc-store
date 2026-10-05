import { Injectable, signal } from '@angular/core';

/** Feeds the app's single status region so screen readers hear outcomes of async actions. */
@Injectable({ providedIn: 'root' })
export class Announcer {
  readonly message = signal('');

  announce(message: string): void {
    // Clearing first makes repeated identical messages announce again.
    this.message.set('');
    queueMicrotask(() => this.message.set(message));
  }
}
