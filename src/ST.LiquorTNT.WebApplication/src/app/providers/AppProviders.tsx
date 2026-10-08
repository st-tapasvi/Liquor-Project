import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClientProvider } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { queryClient, setGlobalMutationErrorListener } from '@/core/api';
import { AuthProvider } from '@/core/auth';
import { isNetworkError } from '@/core/errors';
import { ErrorBoundary } from '@/core/logging';
import { startConnectivityMonitor, useConnectivityStore } from '@/core/network';
import { LoadingFallback } from '@/core/router';
import { theme } from '@/core/theme';

import { ConnectivityToasts } from '@/shared/components/feedback';
import { ConfirmProvider, SnackbarProvider, useSnackbar } from '@/shared/hooks';

import { AppCrash } from '../layout/ErrorPages/AppCrash';

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
              <AuthProvider fallback={<LoadingFallback label="Checking your session…" />}>{children}</AuthProvider>
            </ConfirmProvider>
          </SnackbarProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

function GlobalErrorBridge() {
  const snackbar = useSnackbar();
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
