import { DocumentDetails } from '../app/core/api.models';

export function details(overrides: Partial<DocumentDetails> = {}): DocumentDetails {
  return {
    id: 'doc-1',
    fileName: 'report.pdf',
    contentType: 'application/pdf',
    sizeBytes: 1024,
    uploadedAt: '2026-10-01T12:00:00Z',
    status: 'Ready',
    pageCount: 1,
    title: null,
    author: null,
    textPreview: null,
    access: 'Owner',
    shareLinkToken: null,
    sharedWith: [],
    ...overrides,
  };
}
