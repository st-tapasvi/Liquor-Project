import type { SecurityConfigResponse } from '@/core/api';

import { renderWithProviders, screen, userEvent, waitFor } from '@/test/render';

import { SecurityConfigGrid } from './SecurityConfigGrid';

const ROWS: SecurityConfigResponse[] = [
  {
    key: 'MAX_FAILED_LOGIN_ATTEMPTS',
    value: '3',
    dataType: 'INT',
    description: 'Wrong passwords before the account locks.',
    updatedAt: null,
  },
];

function renderGrid(onSave = vi.fn().mockResolvedValue(undefined)) {
  renderWithProviders(
    <SecurityConfigGrid
      rows={ROWS}
      loading={false}
      error={null}
      onRetry={() => undefined}
      canEdit
      busy={false}
      onSave={onSave}
    />,
  );
  return onSave;
}

describe('SecurityConfigGrid', () => {
  it('lists the settings in a data grid', async () => {
    renderGrid();
    expect(await screen.findByRole('grid', { name: /security settings/i })).toBeInTheDocument();
    expect(screen.getByText('MAX_FAILED_LOGIN_ATTEMPTS')).toBeInTheDocument();
    expect(screen.getByText(/wrong passwords before/i)).toBeInTheDocument();
  });

  it('validates and saves one value', async () => {
    const onSave = renderGrid();
    const user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: /edit max_failed_login_attempts/i }));
    const input = screen.getByRole('textbox', { name: /value for max_failed_login_attempts/i });

    await user.clear(input);
    await user.type(input, 'x');
    await user.click(screen.getByRole('button', { name: /save/i }));
    expect(await screen.findByText(/whole number/i)).toBeInTheDocument();
    expect(onSave).not.toHaveBeenCalled();

    await user.clear(input);
    await user.type(input, '5');
    await user.click(screen.getByRole('button', { name: /save/i }));
    await waitFor(() => expect(onSave).toHaveBeenCalledWith('MAX_FAILED_LOGIN_ATTEMPTS', '5'));
  });
});
