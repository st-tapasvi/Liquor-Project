import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import { createContext, type ReactNode, useCallback, useContext, useMemo, useRef, useState } from 'react';

export interface ConfirmOptions {
  title: string;
  message: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Destructive actions render the confirm button in error colour. */
  destructive?: boolean;
}

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn | null>(null);

/** `const ok = await confirm({ title, message }); if (!ok) return;` – one dialog for the whole app. */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null);
  const resolver = useRef<((value: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>((opts) => {
    setOptions(opts);
    return new Promise<boolean>((resolve) => {
      resolver.current = resolve;
    });
  }, []);

  const settle = (value: boolean) => {
    resolver.current?.(value);
    resolver.current = null;
    setOptions(null);
  };

  const value = useMemo(() => confirm, [confirm]);

  return (
    <ConfirmContext.Provider value={value}>
      {children}
      <Dialog
        open={options !== null}
        onClose={() => settle(false)}
        aria-labelledby="confirm-title"
        maxWidth="xs"
        fullWidth
      >
        {options && (
          <>
            <DialogTitle id="confirm-title">{options.title}</DialogTitle>
            <DialogContent>
              <DialogContentText component="div">{options.message}</DialogContentText>
            </DialogContent>
            <DialogActions>
              <Button onClick={() => settle(false)}>{options.cancelLabel ?? 'Cancel'}</Button>
              <Button
                onClick={() => settle(true)}
                variant="contained"
                color={options.destructive ? 'error' : 'primary'}
                autoFocus
              >
                {options.confirmLabel ?? 'Confirm'}
              </Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </ConfirmContext.Provider>
  );
}

export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext);
  if (!ctx) throw new Error('useConfirm must be used inside <ConfirmProvider>');
  return ctx;
}
