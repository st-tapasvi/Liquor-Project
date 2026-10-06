import { act } from '@testing-library/react';

import { useConnectivityStore } from '@/core/network';

import { renderWithProviders, screen, waitFor } from '@/test/render';

import { ConnectivityToasts } from './ConnectivityToasts';

describe('ConnectivityToasts', () => {
  it('shows nothing at start-up while online', () => {
    renderWithProviders(<ConnectivityToasts />);
    expect(screen.queryByText(/no connection/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/back online/i)).not.toBeInTheDocument();
  });

  it('keeps an offline notice up, then turns it into "Back online"', async () => {
    renderWithProviders(<ConnectivityToasts />);

    act(() => useConnectivityStore.getState().markOffline());
    const offline = await screen.findByText(/no connection to the server/i);
    expect(offline.closest('[role="alert"]')).not.toBeNull();
    expect(screen.queryByRole('button', { name: /dismiss/i })).not.toBeInTheDocument();

    act(() => useConnectivityStore.getState().markOnline());
    expect(await screen.findByText(/back online/i)).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText(/no connection to the server/i)).not.toBeInTheDocument());
  });
});
