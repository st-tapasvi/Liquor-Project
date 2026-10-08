import { usePermission } from '@/core/auth';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { PageHeader } from '@/shared/components/ui';

import { usePasswordPolicies, useUpdatePasswordPolicy } from '../api/settings.queries';
import { PasswordPolicyForm } from '../components/PasswordPolicyForm';

export default function PasswordPoliciesPage() {
  const policies = usePasswordPolicies();
  const update = useUpdatePasswordPolicy();
  const canEdit = usePermission('passwordpolicy.edit');

  return (
    <>
      <PageHeader
        title="Password policies"
        subtitle="Rules applied when a password is set. Existing passwords keep working until changed."
      />
      {policies.isPending && <LoadingOverlay />}
      {policies.isError && <ErrorState error={policies.error} onRetry={() => void policies.refetch()} />}
      {policies.data?.map((policy) => (
        <PasswordPolicyForm
          key={policy.id}
          policy={policy}
          canEdit={canEdit}
          busy={update.isPending}
          onSave={async (id, values) => {
            await update.mutateAsync({ id, request: values });
          }}
        />
      ))}
    </>
  );
}
