import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClientProvider } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { queryClient, setGlobalMutationErrorListener } from '@/core/api';
import { AuthProvider } from '@/core/auth';
import { isNetworkError } from '@/core/errors';
import { ErrorBoundary } from '@/core/logging';
import { startConnectivityMonitor, useConnectivityStore } from '@/core/network';
import { PlatformProvider } from '@/core/platform';
import { LoadingFallback } from '@/core/router';
import { theme } from '@/core/theme';

import { ConnectivityToasts } from '@/shared/components/feedback';
import { ConfirmProvider, SnackbarProvider, useSnackbar } from '@/shared/hooks';

import { AppCrash } from '../layout/ErrorPages/AppCrash';

/**
 * Provider order (mandatory):
 * ErrorBoundary → Theme → QueryClient → Snackbar → Confirm → Auth → Platform → children
 * Theme first so every fallback renders styled; QueryClient before Auth (Auth clears the cache);
 * Snackbar before Auth so the global mutation handler can reach it; Platform inside Auth because the
 * installation's modules and the user's companies are only knowable once there is a session.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary scope="app" fallback={({ error, reset }) => <AppCrash error={error} onReset={reset} />}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <QueryClientProvider client={queryClient}>
          <SnackbarProvider>
            <ConfirmProvider>
              <GlobalErrorBridge />
              <ConnectivityBridge />
              <ConnectivityToasts />
              <AuthProvider fallback={<LoadingFallback label="Checking your session…" />}>
                <PlatformProvider fallback={<LoadingFallback label="Loading your workspace…" />}>
                  {children}
                </PlatformProvider>
              </AuthProvider>
            </ConfirmProvider>
          </SnackbarProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

/** Connects the query client's global mutation errors to the snackbar (core cannot import UI). */
function GlobalErrorBridge() {
  const snackbar = useSnackbar();
  // Why an effect: registers a listener on a module-level singleton and removes it on unmount.
  useEffect(() => {
    setGlobalMutationErrorListener((error) => {
      if (isNetworkError(error) && !error.isTimeout) return;
      snackbar.errorFrom(error);
    });
    return () => setGlobalMutationErrorListener(null);
  }, [snackbar]);
  return null;
}

function ConnectivityBridge() {
  // Why an effect: starts a background watcher (timers, window listeners) and stops it on unmount.
  useEffect(() => {
    const stop = startConnectivityMonitor();
    const unsubscribe = useConnectivityStore.subscribe((state, previous) => {
      if (state.status === 'online' && previous.status === 'offline') {
        void queryClient.refetchQueries({ predicate: (query) => query.state.status === 'error' });
      }
    });
    return () => {
      unsubscribe();
      stop();
    };
  }, []);
  return null;
}
