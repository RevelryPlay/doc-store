import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DocumentDetails } from '../core/api.models';
import { details } from '../../testing/fixtures';
import { DocumentsApi, PROCESSING_POLL_MS } from './documents-api';

describe('DocumentsApi', () => {
  let api: DocumentsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(DocumentsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('sends the link token when reading a document opened by link', () => {
    api.get('doc-1', 'token-abc').subscribe();

    const request = http.expectOne((r) => r.url === '/api/documents/doc-1');
    expect(request.request.params.get('link')).toBe('token-abc');
    request.flush(details({ status: 'Ready' }));
  });

  it('builds download URLs with and without a link token', () => {
    expect(api.contentUrl('doc-1')).toBe('/api/documents/doc-1/content');
    expect(api.contentUrl('doc-1', 'tok_en-1')).toBe('/api/documents/doc-1/content?link=tok_en-1');
  });

  it('shares with PUT and unshares with DELETE on the same resource', () => {
    api.share('doc-1', 'user-2').subscribe();
    api.unshare('doc-1', 'user-2').subscribe();

    const [share, unshare] = http.match('/api/documents/doc-1/shares/user-2');
    expect(share.request.method).toBe('PUT');
    expect(unshare.request.method).toBe('DELETE');
    share.flush(null);
    unshare.flush(null);
  });

  it('uploads the file as multipart form data and returns the new id', () => {
    let id: string | undefined;
    api.upload(new File(['%PDF-1.7'], 'report.pdf')).subscribe((value) => (id = value));

    const request = http.expectOne('/api/documents');
    expect(request.request.method).toBe('POST');
    expect((request.request.body as FormData).get('file')).toBeInstanceOf(File);
    request.flush({ id: 'new-id' });
    expect(id).toBe('new-id');
  });

  it('polls until processing finishes, then emits once and stops', () => {
    vi.useFakeTimers();
    const emitted: DocumentDetails[] = [];
    api.waitForProcessing('doc-1').subscribe((document) => emitted.push(document));

    vi.advanceTimersByTime(0);
    http.expectOne('/api/documents/doc-1').flush(details({ status: 'Pending' }));
    expect(emitted).toEqual([]);

    vi.advanceTimersByTime(PROCESSING_POLL_MS);
    http.expectOne('/api/documents/doc-1').flush(details({ status: 'Ready', pageCount: 2 }));
    expect(emitted).toHaveLength(1);
    expect(emitted[0].pageCount).toBe(2);

    vi.advanceTimersByTime(PROCESSING_POLL_MS * 3);
    http.expectNone('/api/documents/doc-1');
  });
});
