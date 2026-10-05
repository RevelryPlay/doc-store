export type ProcessingStatus = 'Pending' | 'Ready' | 'Failed';
export type AccessLevel = 'None' | 'LinkHolder' | 'SharedUser' | 'Owner';

export interface SessionUser {
  id: string;
  displayName: string;
}

export interface UserSummary {
  id: string;
  displayName: string;
}

export interface DocumentListItem {
  id: string;
  fileName: string;
  sizeBytes: number;
  uploadedAt: string;
  status: ProcessingStatus;
  pageCount: number | null;
  ownerName: string;
  shareCount: number;
  isLinkShared: boolean;
}

export interface DocumentLibrary {
  owned: DocumentListItem[];
  sharedWithMe: DocumentListItem[];
}

export interface DocumentDetails {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
  status: ProcessingStatus;
  pageCount: number | null;
  title: string | null;
  author: string | null;
  textPreview: string | null;
  access: AccessLevel;
  shareLinkToken: string | null;
  sharedWith: string[];
}

export interface ProblemDetails {
  detail?: string;
  title?: string;
}
