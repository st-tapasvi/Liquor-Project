import { usePermission } from '@/core/auth';

import { PageHeader } from '@/shared/components/ui';

import { useSecurityConfig, useUpdateSecurityConfig } from '../api/settings.queries';
import { SecurityConfigGrid } from '../components/SecurityConfigGrid';

export default function SecuritySettingsPage() {
  const config = useSecurityConfig();
  const update = useUpdateSecurityConfig();
  const canEdit = usePermission('securityconfig.edit');

  return (
    <>
      <PageHeader
        title="Security settings"
        subtitle="Login locking, session limits and password recovery. Changes apply from the next request."
      />
      <SecurityConfigGrid
        rows={config.data}
        loading={config.isPending}
        error={config.error}
        onRetry={() => void config.refetch()}
        canEdit={canEdit}
        busy={update.isPending}
        onSave={async (key, value) => {
          await update.mutateAsync({ key, request: { value } });
        }}
      />
    </>
  );
}
