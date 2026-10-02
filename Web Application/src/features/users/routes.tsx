import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

export const userRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.users.list,
    page: () => import('./pages/UserListPage'),
    permission: ['users.view', 'users.manage'],
    module: 'users',
  }),
  createProtectedRoute({
    path: PATHS.users.new,
    page: () => import('./pages/UserEditPage'),
    permission: 'users.manage',
    module: 'users',
  }),
  createProtectedRoute({
    path: PATHS.users.edit(),
    page: () => import('./pages/UserEditPage'),
    permission: ['users.view', 'users.manage'],
    module: 'users',
  }),
];
