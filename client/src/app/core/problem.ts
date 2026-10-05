import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './api.models';

export function problemMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    return problem?.detail ?? problem?.title ?? fallback;
  }
  return fallback;
}
