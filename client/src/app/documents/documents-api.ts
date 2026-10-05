import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, last, map, switchMap, takeWhile, timer } from 'rxjs';
import { DocumentDetails, DocumentLibrary, UserSummary } from '../core/api.models';

export const PROCESSING_POLL_MS = 1000;

@Injectable({ providedIn: 'root' })
export class DocumentsApi {
  private readonly http = inject(HttpClient);

  library(): Observable<DocumentLibrary> {
    return this.http.get<DocumentLibrary>('/api/documents');
  }

  get(id: string, linkToken?: string): Observable<DocumentDetails> {
    return this.http.get<DocumentDetails>(`/api/documents/${id}`, {
      params: linkParams(linkToken),
    });
  }

  getByLink(token: string): Observable<DocumentDetails> {
    return this.http.get<DocumentDetails>(`/api/links/${encodeURIComponent(token)}`);
  }

  contentUrl(id: string, linkToken?: string): string {
    const query = linkParams(linkToken).toString();
    return `/api/documents/${id}/content${query ? `?${query}` : ''}`;
  }

  upload(file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http
      .post<{ id: string }>('/api/documents', form)
      .pipe(map((created) => created.id));
  }

  /** Emits once, with the document after background processing has finished or failed. */
  waitForProcessing(id: string): Observable<DocumentDetails> {
    return timer(0, PROCESSING_POLL_MS).pipe(
      switchMap(() => this.get(id)),
      takeWhile((details) => details.status === 'Pending', true),
      last(),
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/documents/${id}`);
  }

  share(id: string, userId: string): Observable<void> {
    return this.http.put<void>(`/api/documents/${id}/shares/${userId}`, null);
  }

  unshare(id: string, userId: string): Observable<void> {
    return this.http.delete<void>(`/api/documents/${id}/shares/${userId}`);
  }

  enableLink(id: string): Observable<string> {
    return this.http
      .put<{ token: string }>(`/api/documents/${id}/link`, null)
      .pipe(map((response) => response.token));
  }

  disableLink(id: string): Observable<void> {
    return this.http.delete<void>(`/api/documents/${id}/link`);
  }

  users(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>('/api/users');
  }
}

function linkParams(linkToken?: string): HttpParams {
  return linkToken ? new HttpParams().set('link', linkToken) : new HttpParams();
}
