import Button from '@mui/material/Button';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import type { SessionResponse } from '@/core/api';
import { useCurrentUser } from '@/core/auth';

import { AppDataGrid, type AppGridColumn } from '@/shared/components/data-grid';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useConfirm, useSnackbar } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { authFeatureApi } from '../api/auth-feature.api';

const sessionKeys = {
  all: (userId: number) => ['sessions', userId] as const,
};

export default function MySessionsPage() {
  const user = useCurrentUser();
  const qc = useQueryClient();
  const confirm = useConfirm();
  const snackbar = useSnackbar();

  const sessions = useQuery({ queryKey: sessionKeys.all(user.userId), queryFn: authFeatureApi.mySessions });

  const revoke = useMutation({
    mutationKey: ['sessions', 'revoke'],
    mutationFn: authFeatureApi.revokeSession,
    onSuccess: async (result) => {
      snackbar.success(result.message);
      await qc.invalidateQueries({ queryKey: sessionKeys.all(user.userId) });
    },
  });

  const handleRevoke = async (id: number) => {
    const ok = await confirm({
      title: 'Log out this device?',
      message: `Session ${id} will end immediately.`,
      confirmLabel: 'Log out',
      destructive: true,
    });
    if (ok) revoke.mutate(id);
  };

  const columns: AppGridColumn<SessionResponse>[] = [
    {
      field: 'loginAt',
      headerName: 'Signed in',
      rowHeader: true,
      width: 170,
      valueFormatter: (v: string) => formatDateTime(v),
    },
    {
      field: 'lastActivityAt',
      headerName: 'Last activity',
      width: 170,
      valueFormatter: (v: string | null) => formatDateTime(v),
    },
    { field: 'expiresAt', headerName: 'Ends at', width: 170, valueFormatter: (v: string) => formatDateTime(v) },
    { field: 'ipAddress', headerName: 'IP address', width: 150, valueGetter: (v: string | null) => v ?? '' },
    {
      field: 'userAgent',
      headerName: 'Browser',
      flex: 1,
      minWidth: 220,
      valueGetter: (v: string | null) => v ?? '',
      renderCell: ({ value }) => <span title={String(value)}>{String(value)}</span>,
    },
    {
      field: 'actions',
      disableColumnMenu: true,
      headerName: '',
      width: 130,
      sortable: false,
      align: 'right',
      renderCell: ({ row }) =>
        row.isCurrent ? (
          <StatusChip label="This device" tone="info" />
        ) : (
          <Button size="small" color="error" onClick={() => void handleRevoke(row.id)} disabled={revoke.isPending}>
            Log out
          </Button>
        ),
    },
  ];

  return (
    <>
      <PageHeader title="My sessions" subtitle="Devices where you are currently signed in." />
      <AppDataGrid<SessionResponse>
        ariaLabel="My sessions"
        columns={columns}
        rows={sessions.data}
        getRowId={(row) => row.id}
        loading={sessions.isPending}
        error={sessions.error}
        onRetry={() => void sessions.refetch()}
      />
    </>
  );
}
