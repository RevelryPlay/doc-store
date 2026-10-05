import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UserSummary } from '../core/api.models';
import { Announcer } from '../core/announcer';
import { Session } from '../core/session';
import { details } from '../../testing/fixtures';
import { ShareDialog } from './share-dialog';

const users: UserSummary[] = [
  { id: 'me', displayName: 'Avery Chen' },
  { id: 'blake', displayName: 'Blake Rivera' },
  { id: 'casey', displayName: 'Casey Morgan' },
];

describe('ShareDialog', () => {
  let fixture: ComponentFixture<ShareDialog>;
  let http: HttpTestingController;
  let announcer: Announcer;
  let changes: number;

  beforeAll(() => {
    // jsdom does not implement modal dialogs.
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.open = true;
    };
    HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
      this.open = false;
    };
  });

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Session, useValue: { user: signal({ id: 'me', displayName: 'Avery Chen' }) } },
      ],
    });
    fixture = TestBed.createComponent(ShareDialog);
    http = TestBed.inject(HttpTestingController);
    announcer = TestBed.inject(Announcer);
    changes = 0;
    fixture.componentInstance.changed.subscribe(() => changes++);

    fixture.componentInstance.open('doc-1');
    http.expectOne('/api/documents/doc-1').flush(details({ sharedWith: ['blake'] }));
    http.expectOne('/api/users').flush(users);
    http.expectOne('/api/documents/doc-1/activity').flush([
      {
        action: 'Viewed',
        actorName: 'Casey Morgan',
        targetName: null,
        occurredAt: '2026-10-01T12:00:00Z',
      },
      {
        action: 'Shared',
        actorName: 'Avery Chen',
        targetName: 'Blake Rivera',
        occurredAt: '2026-10-01T11:00:00Z',
      },
    ]);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('lists everyone except the owner, reflecting current shares', () => {
    expect(labels()).toEqual(['Blake Rivera', 'Casey Morgan']);
    expect(checkbox('share-user-blake').checked).toBe(true);
    expect(checkbox('share-user-casey').checked).toBe(false);
  });

  it('revoking access sends DELETE, announces, and notifies the page', async () => {
    toggle('share-user-blake');

    const request = http.expectOne('/api/documents/doc-1/shares/blake');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    http.expectOne('/api/documents/doc-1/activity').flush([]);
    await fixture.whenStable();
    await Promise.resolve();

    expect(changes).toBe(1);
    expect(announcer.message()).toBe('Removed access for Blake Rivera.');
  });

  it('restores the checkbox when the server rejects a change', async () => {
    toggle('share-user-casey');

    http
      .expectOne('/api/documents/doc-1/shares/casey')
      .flush(
        { detail: 'Only the owner can change this document.' },
        { status: 403, statusText: 'Forbidden' },
      );
    await fixture.whenStable();
    await Promise.resolve();

    expect(checkbox('share-user-casey').checked).toBe(false);
    expect(changes).toBe(0);
    expect(announcer.message()).toBe('Only the owner can change this document.');
  });

  it('turning on link sharing shows a copyable link', async () => {
    toggle('share-link-enabled');

    http.expectOne('/api/documents/doc-1/link').flush({ token: 'abc123' });
    http.expectOne('/api/documents/doc-1/activity').flush([]);
    await fixture.whenStable();

    const link = fixture.nativeElement.querySelector('#share-link-url') as HTMLInputElement;
    expect(link.value).toBe(`${location.origin}/s/abc123`);
  });

  it('shows who viewed the document and other recent activity', () => {
    const entries = Array.from(
      fixture.nativeElement.querySelectorAll('.activity li') as NodeListOf<HTMLLIElement>,
    ).map((item) => item.textContent!.replace(/\s+/g, ' ').trim());

    expect(entries[0]).toMatch(/^Casey Morgan viewed it/);
    expect(entries[1]).toMatch(/^Avery Chen shared it with Blake Rivera/);
  });

  function checkbox(id: string): HTMLInputElement {
    return fixture.nativeElement.querySelector(`#${id}`) as HTMLInputElement;
  }

  function toggle(id: string): void {
    checkbox(id).click();
  }

  function labels(): string[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll(
        'fieldset:first-of-type label',
      ) as NodeListOf<HTMLLabelElement>,
    ).map((label) => label.textContent!.trim());
  }
});
