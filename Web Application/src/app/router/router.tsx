import { createBrowserRouter, Navigate } from 'react-router';

import { PATHS, RouteErrorFallback } from '@/core/router';

import { authProtectedRoutes, authPublicRoutes } from '@/features/auth';
import { batchRoutes } from '@/features/batches';
import { brandRoutes } from '@/features/brands';
import { caseDataRoutes } from '@/features/case-data';
import { codePoolRoutes } from '@/features/code-pool';
import { companyRoutes } from '@/features/company';
import { dashboardRoutes } from '@/features/dashboard';
import { dispatchRoutes } from '@/features/dispatch';
import { licenseRoutes } from '@/features/license';
import { outboxRoutes } from '@/features/outbox';
import { paletteRoutes } from '@/features/palette';
import { planRoutes } from '@/features/plans';
import { plantRoutes } from '@/features/plant';
import { portalSyncRoutes } from '@/features/portal-sync';
import { reportRoutes } from '@/features/reports';
import { settingsRoutes } from '@/features/settings';
import { userRoutes } from '@/features/users';

import { AppShell, AuthLayout, Forbidden, NotFound } from '../layout';

/**
 * Assembles feature route arrays under the two layouts. Feature routes are never defined here — each
 * module owns its own `routes.tsx` (specification §5, §14.1).
 *
 * All seventeen modules are registered unconditionally. Flag-controlled modules carry `module:` in
 * their route definition, so an installation that does not have one resolves its address to
 * page-not-found (§8.2). Registering them all keeps this file stable as modules are built out, and
 * every page is a lazy chunk, so a module an installation never opens is never downloaded (§11).
 *
 * The API serves index.html for every non-/api path, so deep links work on reload.
 */
export function createAppRouter() {
  return createBrowserRouter([
    { element: <AuthLayout />, errorElement: <RouteErrorFallback />, children: authPublicRoutes },
    {
      element: <AppShell />,
      errorElement: <RouteErrorFallback />,
      children: [
        { path: PATHS.root, element: <Navigate to={PATHS.dashboard} replace /> },

        ...dashboardRoutes,

        // Masters
        ...companyRoutes,
        ...plantRoutes,
        ...userRoutes,
        ...brandRoutes,
        ...batchRoutes,
        ...planRoutes,
        ...paletteRoutes,

        // Production and traceability
        ...codePoolRoutes,
        ...caseDataRoutes,
        ...dispatchRoutes,

        // Integration
        ...portalSyncRoutes,
        ...outboxRoutes,

        // Reporting and administration
        ...reportRoutes,
        ...settingsRoutes,
        ...licenseRoutes,

        ...authProtectedRoutes,
        { path: PATHS.forbidden, element: <Forbidden /> },
        { path: '*', element: <NotFound /> },
      ],
    },
  ]);
}
