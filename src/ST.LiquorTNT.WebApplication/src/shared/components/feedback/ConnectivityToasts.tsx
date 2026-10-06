import CloudDoneRounded from '@mui/icons-material/CloudDoneRounded';
import CloudOffRounded from '@mui/icons-material/CloudOffRounded';
import { useEffect, useRef } from 'react';
import toast from 'react-hot-toast';

import { useConnectivity } from '@/core/network';

export const CONNECTIVITY_TOAST_ID = 'connectivity';

const BAR_STYLE = {
  background: '#262628',
  color: '#fff',
  borderLeft: 'none',
  borderRadius: 999,
  padding: '8px 18px 8px 14px',
  fontSize: 14,
} as const;

export function ConnectivityToasts() {
  const status = useConnectivity();
  const wasOffline = useRef(false);

  useEffect(() => {
    if (status === 'offline') {
      wasOffline.current = true;
      toast('No connection to the server. Trying to reconnect…', {
        id: CONNECTIVITY_TOAST_ID,
        duration: Infinity,
        style: BAR_STYLE,
        icon: <CloudOffRounded fontSize="small" sx={{ color: '#ff8a80' }} />,
        ariaProps: { role: 'alert', 'aria-live': 'assertive' },
      });
      return;
    }

    if (wasOffline.current) {
      wasOffline.current = false;
      toast('Back online', {
        id: CONNECTIVITY_TOAST_ID,
        duration: 3000,
        style: BAR_STYLE,
        icon: <CloudDoneRounded fontSize="small" sx={{ color: '#69f0ae' }} />,
        ariaProps: { role: 'status', 'aria-live': 'polite' },
      });
    }
  }, [status]);

  return null;
}
