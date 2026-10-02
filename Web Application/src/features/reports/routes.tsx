import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the reports module. `module: 'reports'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const reportRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.reports.activity,
    page: () => import('./ReportsPage'),
    permission: ['reports.view', 'reports.export'],
    module: 'reports',
  }),
];
