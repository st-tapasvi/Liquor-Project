import { QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen, waitFor } from '@testing-library/react';

import { makeCurrentUser } from '@/test/factories/user.factory';

import { createQueryClient } from '../api';
import { NetworkError, ApiError } from '../errors';
import { useConnectivityStore } from '../network';

import { authApi } from './auth.api';
import { AuthProvider } from './AuthProvider';
import { useSessionStore } from './session.store';

function renderProvider() {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <AuthProvider fallback={<div>checking</div>}>
        <div>app</div>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe('AuthProvider', () => {
  it('shows the app as anonymous/unreachable when the API is down at start-up', async () => {
    vi.spyOn(authApi, 'me').mockRejectedValue(new NetworkError('Network Error'));
    renderProvider();
    expect(await screen.findByText('app')).toBeInTheDocument();
    expect(useSessionStore.getState()).toMatchObject({ status: 'anonymous', endReason: 'unreachable' });
  });

  it('restores a still-valid session in the background once the API is back', async () => {
    const me = vi
      .spyOn(authApi, 'me')
      .mockRejectedValueOnce(new NetworkError('Network Error'))
      .mockResolvedValueOnce(makeCurrentUser({ userName: 'admin' }));
    act(() => useConnectivityStore.getState().markOffline());
    renderProvider();
    await waitFor(() => expect(useSessionStore.getState().endReason).toBe('unreachable'));

    act(() => useConnectivityStore.getState().markOnline());

    await waitFor(() => expect(useSessionStore.getState().status).toBe('authenticated'));
    expect(me).toHaveBeenCalledTimes(2);
    expect(screen.getByText('app')).toBeInTheDocument(); // never went back to the loading fallback
  });

  it('drops the "unreachable" reason when the re-check says there is no session', async () => {
    vi.spyOn(authApi, 'me')
      .mockRejectedValueOnce(new NetworkError('Network Error'))
      .mockRejectedValueOnce(new ApiError({ status: 401, code: 'UNAUTHENTICATED', title: 'No session.' }));
    act(() => useConnectivityStore.getState().markOffline());
    renderProvider();
    await waitFor(() => expect(useSessionStore.getState().endReason).toBe('unreachable'));

    act(() => useConnectivityStore.getState().markOnline());

    await waitFor(() => expect(useSessionStore.getState().endReason).toBeNull());
    expect(useSessionStore.getState().status).toBe('anonymous');
  });
});
