import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

export const settingsRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.settings.security,
    page: () => import('./pages/SecuritySettingsPage'),
    permission: 'securityconfig.view',
  }),
  createProtectedRoute({
    path: PATHS.settings.passwordPolicies,
    page: () => import('./pages/PasswordPoliciesPage'),
    permission: 'passwordpolicy.view',
  }),
];
