import { createBrowserRouter, Navigate } from 'react-router';

import { PATHS, RouteErrorFallback } from '@/core/router';

import { authProtectedRoutes, authPublicRoutes } from '@/features/auth';
import { companyRoutes } from '@/features/company';
import { dashboardRoutes } from '@/features/dashboard';
import { settingsRoutes } from '@/features/settings';
import { userRoutes } from '@/features/users';

import { AppShell, AuthLayout, Forbidden, NotFound } from '../layout';

export function createAppRouter() {
  return createBrowserRouter([
    { element: <AuthLayout />, errorElement: <RouteErrorFallback />, children: authPublicRoutes },
    {
      element: <AppShell />,
      errorElement: <RouteErrorFallback />,
      children: [
        { path: PATHS.root, element: <Navigate to={PATHS.dashboard} replace /> },

        ...dashboardRoutes,
        ...companyRoutes,
        ...userRoutes,
        ...settingsRoutes,
        ...authProtectedRoutes,
        { path: PATHS.forbidden, element: <Forbidden /> },
        { path: '*', element: <NotFound /> },
      ],
    },
  ]);
}
