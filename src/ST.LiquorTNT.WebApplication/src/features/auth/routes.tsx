import type { RouteObject } from 'react-router';

import { createProtectedRoute, createPublicRoute, PATHS } from '@/core/router';

/** Anonymous screens, rendered inside AuthLayout. */
export const authPublicRoutes: RouteObject[] = [
  createPublicRoute({ path: PATHS.login, page: () => import('./pages/LoginPage') }),
  createPublicRoute({ path: PATHS.changePassword, page: () => import('./pages/ChangePasswordPage') }),
  createPublicRoute({ path: PATHS.forgotPassword, page: () => import('./pages/ForgotPasswordPage') }),
];

/** Screens about the user's own session, rendered inside AppShell. Every logged-in user may open them. */
export const authProtectedRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.settings.sessions,
    page: () => import('./pages/MySessionsPage'),
    module: 'settings',
  }),
];
