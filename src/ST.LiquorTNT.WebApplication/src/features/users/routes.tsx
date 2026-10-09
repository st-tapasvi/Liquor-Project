import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

export const userRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.users.list,
    page: () => import('./pages/UserListPage'),
    permission: 'user.view',
  }),
  createProtectedRoute({
    path: PATHS.users.new,
    page: () => import('./pages/UserEditPage'),
    permission: 'user.add',
  }),
  createProtectedRoute({
    path: PATHS.users.view(),
    page: () => import('./pages/UserEditPage'),
    permission: 'user.view',
  }),
  createProtectedRoute({
    path: PATHS.users.edit(),
    page: () => import('./pages/UserEditPage'),
    permission: 'user.edit',
  }),
  createProtectedRoute({
    path: PATHS.users.roles,
    page: () => import('./pages/RoleListPage'),
    permission: 'role.view',
  }),
];
