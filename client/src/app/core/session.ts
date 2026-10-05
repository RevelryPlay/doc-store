import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { SessionUser } from './api.models';

@Injectable({ providedIn: 'root' })
export class Session {
  private readonly http = inject(HttpClient);
  private readonly current = signal<SessionUser | null>(null);

  readonly user = this.current.asReadonly();
  readonly isSignedIn = computed(() => this.current() !== null);

  /** Also refreshes the antiforgery cookie, which is bound to the current identity. */
  refresh(): Observable<SessionUser | null> {
    return this.http.get<SessionUser>('/api/auth/me').pipe(
      catchError((error: HttpErrorResponse) =>
        error.status === 401 ? of(null) : throwError(() => error),
      ),
      tap((user) => this.current.set(user)),
    );
  }

  signIn(userName: string, password: string): Observable<SessionUser> {
    return this.http
      .post<SessionUser>('/api/auth/login', { userName, password })
      .pipe(tap((user) => this.current.set(user)));
  }

  signOut(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null).pipe(
      tap(() => this.current.set(null)),
      map(() => undefined),
    );
  }

  clear(): void {
    this.current.set(null);
  }
}
