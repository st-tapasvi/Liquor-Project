import CloseRounded from '@mui/icons-material/CloseRounded';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import WarningAmberRounded from '@mui/icons-material/WarningAmberRounded';
import IconButton from '@mui/material/IconButton';
import type { CSSProperties, ReactElement, ReactNode } from 'react';
import toast, { ToastBar, Toaster } from 'react-hot-toast';

import { describeError, isApiError, isClientError } from '@/core/errors';
import { tokens } from '@/core/theme';

import { CONNECTIVITY_TOAST_ID } from '../components/feedback/ConnectivityToasts';

export type ToastSeverity = 'success' | 'info' | 'warning' | 'error';

export interface ToastOptions {
  id?: string;
}

export interface SnackbarApi {
  success: (text: string, options?: ToastOptions) => void;
  info: (text: string, options?: ToastOptions) => void;
  warning: (text: string, options?: ToastOptions) => void;
  error: (text: string, reference?: string, options?: ToastOptions) => void;
  errorFrom: (error: unknown, options?: ToastOptions) => void;
  dismiss: (id?: string) => void;
}

export const PERSISTENT_TOAST_IDS: ReadonlySet<string> = new Set([CONNECTIVITY_TOAST_ID]);

const DURATION_MS: Record<ToastSeverity, number> = { success: 4000, info: 4000, warning: 6000, error: 8000 };

const accent = (color: string): CSSProperties => ({ borderLeft: `4px solid ${color}` });

function content(text: string, reference?: string, heading?: string): string | ReactElement {
  if (!reference && !heading) return text;
  return (
    <span>
      {heading && <strong style={{ display: 'block', fontWeight: 600, marginBottom: 2 }}>{heading}</strong>}
      {text}
      {reference && (
        <span style={{ display: 'block', opacity: 0.7, fontSize: tokens.font.caption, marginTop: 2 }}>
          Ref: {reference}
        </span>
      )}
    </span>
  );
}

function show(
  severity: ToastSeverity,
  text: string,
  reference?: string,
  options?: ToastOptions & { heading?: string },
): void {
  const base = { duration: DURATION_MS[severity], ...(options?.id === undefined ? {} : { id: options.id }) };
  const message = content(text, reference, options?.heading);
  switch (severity) {
    case 'success':
      toast.success(message, base);
      return;
    case 'error':
      toast.error(message, { ...base, ariaProps: { role: 'alert', 'aria-live': 'assertive' } });
      return;
    case 'warning':
      toast(message, {
        ...base,
        style: accent(tokens.color.warning),
        icon: <WarningAmberRounded sx={{ color: 'warning.main' }} fontSize="small" />,
      });
      return;
    case 'info':
      toast(message, { ...base, icon: <InfoOutlined sx={{ color: 'info.main' }} fontSize="small" /> });
  }
}

export const snackbar: SnackbarApi = {
  success: (text, options) => show('success', text, undefined, options),
  info: (text, options) => show('info', text, undefined, options),
  warning: (text, options) => show('warning', text, undefined, options),
  error: (text, reference, options) => show('error', text, reference, options),
  errorFrom: (error, options) => {
    if (!isApiError(error)) {
      show('error', describeError(error), undefined, options);
      return;
    }
    const heading = error.detail && error.detail !== error.title ? error.title : undefined;
    const reference = !isClientError(error) || error.code === 'UNKNOWN' ? error.correlationId : undefined;
    show('error', describeError(error), reference, { ...options, ...(heading ? { heading } : {}) });
  },
  dismiss: (id) => toast.dismiss(id),
};

export function useSnackbar(): SnackbarApi {
  return snackbar;
}

export function SnackbarProvider({ children }: { children: ReactNode }) {
  return (
    <>
      {children}
      <AppToaster />
    </>
  );
}

function AppToaster() {
  return (
    <Toaster
      position="bottom-right"
      gutter={10}
      containerStyle={{ right: 24, bottom: 24 }}
      toastOptions={{
        style: {
          fontFamily: tokens.fontFamily,
          fontSize: tokens.font.body2,
          lineHeight: 1.45,
          color: tokens.color.textPrimary,
          background: tokens.color.surface,
          borderRadius: 10,
          boxShadow: '0 6px 24px rgba(28, 28, 30, 0.14), 0 1px 3px rgba(28, 28, 30, 0.08)',
          padding: '10px 8px 10px 14px',
          maxWidth: 460,
          ...accent(tokens.color.info),
        },
        success: {
          style: accent(tokens.color.success),
          iconTheme: { primary: tokens.color.success, secondary: '#fff' },
        },
        error: {
          style: accent(tokens.color.error),
          iconTheme: { primary: tokens.color.error, secondary: '#fff' },
        },
      }}
    >
      {(t) => (
        <ToastBar toast={t}>
          {({ icon, message }) => (
            <>
              {icon}
              {message}
              {t.type !== 'loading' && !PERSISTENT_TOAST_IDS.has(t.id) && (
                <IconButton
                  size="small"
                  aria-label="Dismiss notification"
                  onClick={() => toast.dismiss(t.id)}
                  sx={{ ml: 0.5, alignSelf: 'flex-start', color: 'text.secondary' }}
                >
                  <CloseRounded fontSize="small" />
                </IconButton>
              )}
            </>
          )}
        </ToastBar>
      )}
    </Toaster>
  );
}
