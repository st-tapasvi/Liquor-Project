import { type ComponentType, lazy, Suspense } from 'react';
import type { RouteObject } from 'react-router';

import type { PermissionKey } from '../auth/permissions';
import type { ModuleKey } from '../modules';

import { RequireModule } from './guards/RequireModule';
import { RequirePermission } from './guards/RequirePermission';
import { LoadingFallback } from './LoadingFallback';
import { RouteErrorFallback } from './RouteErrorFallback';

type PageLoader = () => Promise<{ default: ComponentType }>;

interface ProtectedRouteOptions {
  /** Absolute path from PATHS. */
  path: string;
  /** Dynamic import of the page; the page file must `export default`. */
  page: PageLoader;
  /** Right(s) required to open the page. Omit only for pages every logged-in user may open. */
  permission?: PermissionKey | readonly PermissionKey[];
  /**
   * The module this screen belongs to. Required for flag-controlled modules (§8.2): when the
   * installation does not have the module the route resolves to page-not-found. Safe to pass for
   * always-present modules too — the flag is not consulted for those.
   */
  module?: ModuleKey;
  children?: RouteObject[];
}

/** Lazy page + Suspense. Every page is its own chunk, so a module never lands in the initial bundle. */
const RETRY_DELAY_MS = 600;

function withRetry(loader: PageLoader): PageLoader {
  return () =>
    loader().catch(
      () =>
        new Promise<{ default: ComponentType }>((resolve, reject) => {
          setTimeout(() => {
            loader().then(resolve, reject);
          }, RETRY_DELAY_MS);
        }),
    );
}

function lazyPage(loader: PageLoader) {
  const Page = lazy(withRetry(loader));
  return (
    <Suspense fallback={<LoadingFallback />}>
      <Page />
    </Suspense>
  );
}

/** The only way features declare authenticated routes: lazy, guarded, with a route error boundary. */
export function createProtectedRoute({ path, page, permission, module, children }: ProtectedRouteOptions): RouteObject {
  let element = lazyPage(page);
  // Innermost first: the right is only worth checking once the module exists at all.
  if (permission) element = <RequirePermission right={permission}>{element}</RequirePermission>;
  if (module) element = <RequireModule module={module}>{element}</RequireModule>;
  return { path, element, errorElement: <RouteErrorFallback />, ...(children ? { children } : {}) };
}

/** For anonymous pages (login, forgot password, forced password change). */
export function createPublicRoute({ path, page }: Pick<ProtectedRouteOptions, 'path' | 'page'>): RouteObject {
  return { path, element: lazyPage(page), errorElement: <RouteErrorFallback /> };
}
