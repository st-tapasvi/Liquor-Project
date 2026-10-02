import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

export const dashboardRoutes: RouteObject[] = [
  createProtectedRoute({ path: PATHS.dashboard, page: () => import('./DashboardPage'), module: 'dashboard' }),
];
