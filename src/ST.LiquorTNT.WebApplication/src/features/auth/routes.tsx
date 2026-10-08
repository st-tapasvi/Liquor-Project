import type { RouteObject } from 'react-router';

import { createProtectedRoute, createPublicRoute, PATHS } from '@/core/router';

export const authPublicRoutes: RouteObject[] = [
  createPublicRoute({ path: PATHS.login, page: () => import('./pages/LoginPage') }),
  createPublicRoute({ path: PATHS.changePassword, page: () => import('./pages/ChangePasswordPage') }),
  createPublicRoute({ path: PATHS.forgotPassword, page: () => import('./pages/ForgotPasswordPage') }),
];

export const authProtectedRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.settings.sessions,
    page: () => import('./pages/MySessionsPage'),
  }),
];
