import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { problemMessage } from '../core/problem';
import { Session } from '../core/session';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly session = inject(Session);
  private readonly router = inject(Router);

  readonly returnUrl = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    userName: ['', Validators.required],
    password: ['', Validators.required],
  });
  protected readonly submitted = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  constructor() {
    // Fetches a fresh antiforgery token; a stale one from an expired session would reject the login.
    this.session.refresh().subscribe();
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      return;
    }

    const { userName, password } = this.form.getRawValue();
    this.busy.set(true);
    this.error.set(null);
    this.session.signIn(userName, password).subscribe({
      next: () => this.router.navigateByUrl(this.safeReturnUrl()),
      error: (error) => {
        this.busy.set(false);
        this.error.set(problemMessage(error, 'Sign in failed.'));
      },
    });
  }

  protected showError(control: 'userName' | 'password'): boolean {
    return this.submitted() && this.form.controls[control].invalid;
  }

  // Only same-site paths, so the login page can't be used as an open redirect.
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url?.startsWith('/') && !url.startsWith('//') ? url : '/documents';
  }
}
