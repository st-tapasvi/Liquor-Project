import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the portal-sync module. `module: 'portal-sync'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const portalSyncRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.portalSync.list,
    page: () => import('./PortalSyncPage'),
    permission: ['portal-sync.view', 'portal-sync.run'],
    module: 'portal-sync',
  }),
];
