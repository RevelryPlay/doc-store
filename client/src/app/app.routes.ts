import { Routes } from '@angular/router';
import { requireSignIn } from './core/auth';
import { DocumentsPage } from './documents/documents-page';
import { LinkPage } from './links/link-page';
import { LoginPage } from './login/login-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'documents' },
  { path: 'login', component: LoginPage, title: 'Sign in · Doc Store' },
  {
    path: 'documents',
    component: DocumentsPage,
    canActivate: [requireSignIn],
    title: 'Documents · Doc Store',
  },
  {
    path: 's/:token',
    component: LinkPage,
    canActivate: [requireSignIn],
    title: 'Shared document · Doc Store',
  },
  { path: '**', redirectTo: 'documents' },
];
