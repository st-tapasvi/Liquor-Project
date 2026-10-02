import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the outbox module. `module: 'outbox'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const outboxRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.outbox.list,
    page: () => import('./OutboxPage'),
    permission: ['outbox.view', 'outbox.retry'],
    module: 'outbox',
  }),
];
