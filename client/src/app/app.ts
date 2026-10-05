import { Component, ElementRef, Injector, afterNextRender, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, skip } from 'rxjs';
import { Announcer } from './core/announcer';
import { Session } from './core/session';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  protected readonly session = inject(Session);
  protected readonly announcer = inject(Announcer);

  constructor() {
    // A route change in an SPA is silent to screen readers; moving focus to the new
    // page heading announces it and puts keyboard users at the top of the content.
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        skip(1),
        takeUntilDestroyed(),
      )
      .subscribe(() =>
        afterNextRender(() => this.main().nativeElement.querySelector<HTMLElement>('h1')?.focus(), {
          injector: this.injector,
        }),
      );
  }

  protected skipToMain(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }

  protected signOut(): void {
    this.session.signOut().subscribe(() => this.router.navigate(['/login']));
  }
}
