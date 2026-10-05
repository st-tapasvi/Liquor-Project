import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';

import { usePermission } from '@/core/auth';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';
import { PageHeader } from '@/shared/components/ui';

import { useSecurityConfig, useUpdateSecurityConfig } from '../api/settings.queries';
import { SecurityConfigRow } from '../components/SecurityConfigRow';

export default function SecuritySettingsPage() {
  const config = useSecurityConfig();
  const update = useUpdateSecurityConfig();
  const canEdit = usePermission('settings.manage');

  return (
    <>
      <PageHeader
        title="Security settings"
        subtitle="Login locking, session limits and password recovery. Changes apply from the next request."
      />
      <Section dense>
        {config.isPending && <LoadingOverlay />}
        {config.isError && <ErrorState error={config.error} onRetry={() => void config.refetch()} />}
        {config.data && (
          <Table size="small" aria-label="Security settings">
            <TableHead>
              <TableRow>
                <TableCell>Key</TableCell>
                <TableCell>Value</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Updated</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {config.data.map((entry) => (
                <SecurityConfigRow
                  key={entry.key}
                  entry={entry}
                  canEdit={canEdit}
                  busy={update.isPending}
                  onSave={async (key, value) => {
                    await update.mutateAsync({ key, request: { value } });
                  }}
                />
              ))}
            </TableBody>
          </Table>
        )}
      </Section>
    </>
  );
}
