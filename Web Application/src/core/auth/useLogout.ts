import { useMutation } from '@tanstack/react-query';

import { logger } from '../logging/logger';

import { authApi } from './auth.api';
import { useSessionStore } from './session.store';

/** Ends the session on the server, then locally. Local state (and the token) is cleared even if the API call fails. */
export function useLogout() {
  const setAnonymous = useSessionStore((s) => s.setAnonymous);

  return useMutation({
    mutationKey: ['auth', 'logout'],
    meta: { silent: true },
    mutationFn: () => authApi.logout(),
    onSettled: (_data, error) => {
      if (error) logger.warn('logout call failed; clearing local session anyway', { error });
      setAnonymous('logout');
    },
  });
}
