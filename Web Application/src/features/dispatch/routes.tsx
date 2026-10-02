import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the dispatch module. `module: 'dispatch'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const dispatchRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.dispatch.list,
    page: () => import('./DispatchPage'),
    permission: ['dispatch.view', 'dispatch.manage'],
    module: 'dispatch',
  }),
];
