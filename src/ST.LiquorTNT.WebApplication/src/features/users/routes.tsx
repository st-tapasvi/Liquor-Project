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
  createProtectedRoute({
    path: PATHS.users.roleNew,
    page: () => import('./pages/RoleCreatePage'),
    permission: 'role.add',
  }),
  createProtectedRoute({
    path: PATHS.users.roleView(),
    page: () => import('./pages/RoleEditPage'),
    permission: 'role.view',
  }),
  createProtectedRoute({
    path: PATHS.users.roleEdit(),
    page: () => import('./pages/RoleEditPage'),
    permission: 'role.edit',
  }),
  createProtectedRoute({
    path: PATHS.users.roleProfiles,
    page: () => import('./pages/RoleProfileListPage'),
    permission: 'role.view',
  }),
  createProtectedRoute({
    path: PATHS.users.roleProfileNew,
    page: () => import('./pages/RoleProfileEditPage'),
    permission: 'role.add',
  }),
  createProtectedRoute({
    path: PATHS.users.roleProfileView(),
    page: () => import('./pages/RoleProfileEditPage'),
    permission: 'role.view',
  }),
  createProtectedRoute({
    path: PATHS.users.roleProfileEdit(),
    page: () => import('./pages/RoleProfileEditPage'),
    permission: 'role.edit',
  }),
];
