import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the plans module. `module: 'plans'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const planRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.plans.list,
    page: () => import('./PlanPage'),
    permission: ['plans.view', 'plans.manage'],
    module: 'plans',
  }),
];
