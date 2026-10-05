import Button from '@mui/material/Button';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useCurrentUser } from '@/core/auth';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';
import { PageHeader, StatusChip } from '@/shared/components/ui';
import { useConfirm, useSnackbar } from '@/shared/hooks';
import { formatDateTime } from '@/shared/utils';

import { authFeatureApi } from '../api/auth-feature.api';

const sessionKeys = {
  all: (userId: number) => ['sessions', userId] as const,
};

/** "My logged-in devices": lets the user end another device's session (e.g. when the device limit is reached). */
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

  return (
    <>
      <PageHeader title="My sessions" subtitle="Devices where you are currently signed in." />
      <Section>
        {sessions.isPending && <LoadingOverlay />}
        {sessions.isError && <ErrorState error={sessions.error} onRetry={() => void sessions.refetch()} />}
        {sessions.data && (
          <Table size="small" aria-label="My sessions">
            <TableHead>
              <TableRow>
                <TableCell>Signed in</TableCell>
                <TableCell>Last activity</TableCell>
                <TableCell>Ends at</TableCell>
                <TableCell>IP address</TableCell>
                <TableCell>Browser</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {sessions.data.map((s) => (
                <TableRow key={s.id}>
                  <TableCell>{formatDateTime(s.loginAt)}</TableCell>
                  <TableCell>{formatDateTime(s.lastActivityAt)}</TableCell>
                  <TableCell>{formatDateTime(s.expiresAt)}</TableCell>
                  <TableCell>{s.ipAddress ?? ''}</TableCell>
                  <TableCell
                    sx={{ maxWidth: 320, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                    title={s.userAgent ?? ''}
                  >
                    {s.userAgent ?? ''}
                  </TableCell>
                  <TableCell align="right">
                    {s.isCurrent ? (
                      <StatusChip label="This device" tone="info" />
                    ) : (
                      <Button
                        size="small"
                        color="error"
                        onClick={() => void handleRevoke(s.id)}
                        disabled={revoke.isPending}
                      >
                        Log out
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Section>
    </>
  );
}
