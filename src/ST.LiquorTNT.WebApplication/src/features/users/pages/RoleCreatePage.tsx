import { zodResolver } from '@hookform/resolvers/zod';
import Alert from '@mui/material/Alert';
import AlertTitle from '@mui/material/AlertTitle';
import Chip from '@mui/material/Chip';
import Grid from '@mui/material/Grid';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { FormProvider, type Path, useForm, type UseFormReturn, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router';

import type { SupplierCodeResponse } from '@/core/api';
import { useCurrentUser, usePermission } from '@/core/auth';
import { applyServerErrors, describeError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { FormActions, FormRootError, FormSelect, FormTextField } from '@/shared/components/forms';
import { Section } from '@/shared/components/layout';
import { PageHeader } from '@/shared/components/ui';
import { useSnackbar } from '@/shared/hooks';
import type { Option } from '@/shared/types';

import { type CreateRoleOutcome, useCreateRoles } from '../api/users.mutations';
import {
  useCompanyOptions,
  useExciseOptions,
  usePages,
  usePasswordPolicyOptions,
  useSupplierCodeOptions,
} from '../api/users.queries';
import { pagesForExcise } from '../components/role-rights';
import { RolePagesPicker } from '../components/RolePagesPicker';
import {
  ALL_SUPPLIER_CODES,
  type CreateRoleFormInput,
  type CreateRoleFormValues,
  createRoleSchema,
} from '../users.schema';

const NO_RIGHTS: ReadonlySet<number> = new Set();

function useSyncSelect(
  form: UseFormReturn<CreateRoleFormInput, unknown, CreateRoleFormValues>,
  name: Path<CreateRoleFormInput>,
  current: unknown,
  options: readonly Option[],
  autoSelectSingle = true,
) {
  useEffect(() => {
    const only = options.length === 1 ? options[0] : undefined;
    if (autoSelectSingle && only && current !== only.value) {
      form.setValue(name, only.value);
      return;
    }
    if (current !== null && current !== undefined && !options.some((o) => o.value === current)) {
      form.setValue(name, null);
    }
  }, [form, name, current, options, autoSelectSingle]);
}

function distinctBy<T>(items: readonly T[], key: (item: T) => number): T[] {
  const seen = new Set<number>();
  return items.filter((item) => {
    const k = key(item);
    if (seen.has(k)) return false;
    seen.add(k);
    return true;
  });
}

export default function RoleCreatePage() {
  const me = useCurrentUser();
  const navigate = useNavigate();
  const snackbar = useSnackbar();
  const canViewSupplierCodes = usePermission('suppliercode.view');
  const canViewPolicies = usePermission('passwordpolicy.view');
  const canManageAdmins = usePermission('user.manageadmin');

  const form = useForm<CreateRoleFormInput, unknown, CreateRoleFormValues>({
    resolver: zodResolver(createRoleSchema),
    defaultValues: { roleName: '', companyId: null, exciseId: null, supplierCode: null, passwordPolicyId: null },
  });
  const [roleName, companyId, exciseId, supplierCode, passwordPolicyId] = useWatch({
    control: form.control,
    name: ['roleName', 'companyId', 'exciseId', 'supplierCode', 'passwordPolicyId'],
  });

  const companies = useCompanyOptions(me.isSuperAdmin);
  const supplierCodes = useSupplierCodeOptions(canViewSupplierCodes);
  const excises = useExciseOptions(canViewSupplierCodes);
  const policies = usePasswordPolicyOptions(canViewPolicies);
  const pages = usePages();
  const create = useCreateRoles();

  const [selected, setSelected] = useState<Set<number>>(() => new Set());
  const [outcomes, setOutcomes] = useState<CreateRoleOutcome[] | null>(null);

  const activeCodes: SupplierCodeResponse[] = (
    canViewSupplierCodes ? (supplierCodes.data ?? []) : [...me.supplierCodes]
  ).filter((s) => s.isActive);

  const companyOptions: Option<number>[] = me.isSuperAdmin
    ? (companies.data ?? [])
        .filter((c) => c.isActive)
        .map((c) => ({ value: c.id, label: c.companyName ?? `Company ${c.id}` }))
    : distinctBy(activeCodes, (s) => s.companyId).map((s) => ({
        value: s.companyId,
        label: s.companyName ?? `Company ${s.companyId}`,
      }));

  const codesOfCompany = activeCodes.filter((s) => s.companyId === companyId);
  const exciseOptions: Option<number>[] = distinctBy(codesOfCompany, (s) => s.exciseId).map((s) => {
    const name = excises.data?.find((e) => e.id === s.exciseId)?.exciseName;
    return { value: s.exciseId, label: name ? `${s.exciseCode} – ${name}` : s.exciseCode };
  });

  const codesOfExcise = codesOfCompany
    .filter((s) => s.exciseId === exciseId)
    .sort((a, b) => a.displayName.localeCompare(b.displayName));
  const supplierOptions: Option[] = [
    ...(codesOfExcise.length > 1
      ? [{ value: ALL_SUPPLIER_CODES, label: `All supplier codes (${codesOfExcise.length})` }]
      : []),
    ...codesOfExcise.map((s) => ({ value: s.id, label: s.displayName })),
  ];

  const policyOptions: Option<number>[] = (policies.data ?? []).map((p) => ({
    value: p.id,
    label: p.policyName ?? p.name,
  }));

  useSyncSelect(form, 'companyId', companyId, companyOptions);
  useSyncSelect(form, 'exciseId', exciseId, exciseOptions);
  useSyncSelect(form, 'supplierCode', supplierCode, supplierOptions, codesOfExcise.length === 1);
  useSyncSelect(form, 'passwordPolicyId', passwordPolicyId, policyOptions);

  const exciseCode = codesOfCompany.find((s) => s.exciseId === exciseId)?.exciseCode ?? null;
  const visiblePages = pagesForExcise(pages.data ?? [], exciseCode);
  const targets =
    supplierCode === ALL_SUPPLIER_CODES ? codesOfExcise : codesOfExcise.filter((s) => s.id === supplierCode);
  const trimmedName = roleName.trim();

  const submit = form.handleSubmit(async (values) => {
    setOutcomes(null);
    if (targets.length === 0) return;

    const visibleIds = new Set(visiblePages.flatMap((p) => p.actions.map((a) => a.pageActionId)));
    try {
      const result = await create.mutateAsync({
        request: {
          roleName: values.roleName,
          description: null,
          companyId: values.companyId,
          isAdminRole: false,
          perSupplierCode: false,
          passwordPolicyId: values.passwordPolicyId,
        },
        supplierCodes: targets,
        pageActionIds: [...selected].filter((id) => visibleIds.has(id)),
      });

      const failed = result.filter((o) => o.error !== null);
      if (failed.length === 0) {
        snackbar.success(result.length === 1 ? 'Role created.' : `${result.length} roles created.`);
        void navigate(PATHS.users.roles);
        return;
      }
      const single = result.length === 1 ? result[0] : undefined;
      if (single?.role === null && applyServerErrors(form.setError, single.error, { fields: ['roleName'] })) return;
      setOutcomes(result);
    } catch (error) {
      form.setError('root.submit', { type: 'server', message: describeError(error) });
    }
  });

  return (
    <>
      <PageHeader title="New role" subtitle="Choose where the role works, then tick the pages and actions it allows." />
      <FormProvider {...form}>
        <form onSubmit={submit} noValidate>
          <FormRootError />
          {outcomes && <OutcomeList outcomes={outcomes} roleName={trimmedName} />}
          <Section>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 6 }}>
                <FormTextField<CreateRoleFormInput>
                  name="roleName"
                  label="Role name"
                  required
                  autoComplete="off"
                  autoFocus
                  slotProps={{ htmlInput: { maxLength: 100 } }}
                />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <FormSelect<CreateRoleFormInput>
                  name="passwordPolicyId"
                  label="Password policy"
                  required
                  options={policyOptions}
                  helperText={
                    policies.isError || !canViewPolicies ? 'Password policies could not be loaded.' : undefined
                  }
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormSelect<CreateRoleFormInput>
                  name="companyId"
                  label="Company"
                  required
                  options={companyOptions}
                  disabled={companyOptions.length <= 1}
                  helperText={companies.isError ? 'Companies could not be loaded.' : undefined}
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormSelect<CreateRoleFormInput>
                  name="exciseId"
                  label="Excise"
                  required
                  options={exciseOptions}
                  disabled={companyId === null || exciseOptions.length <= 1}
                  helperText={
                    companyId !== null && exciseOptions.length === 0 ? 'No supplier code in this company.' : undefined
                  }
                />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormSelect<CreateRoleFormInput>
                  name="supplierCode"
                  label="Supplier code"
                  required
                  options={supplierOptions}
                  disabled={exciseId === null || supplierOptions.length <= 1}
                  helperText={supplierCodes.isError ? 'Supplier codes could not be loaded.' : undefined}
                />
              </Grid>
            </Grid>
            {targets.length > 0 && (
              <Stack direction="row" spacing={1} useFlexGap sx={{ mt: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                <Typography variant="body2" color="text.secondary">
                  {targets.length === 1 ? 'Role to be created:' : `${targets.length} roles to be created:`}
                </Typography>
                {targets.map((s) => (
                  <Chip
                    key={s.id}
                    size="small"
                    variant={trimmedName ? 'filled' : 'outlined'}
                    sx={{
                      borderRadius: 1,
                      fontWeight: 600,
                      color: 'text.primary',
                      border: '1px solid #C5C6CA',
                      ...(trimmedName ? { bgcolor: '#EDEEF0' } : { bgcolor: 'transparent', borderStyle: 'dashed' }),
                    }}
                    label={`${trimmedName || 'Role name'} - ${s.displayName}`}
                  />
                ))}
              </Stack>
            )}
          </Section>

          <Section title="Pages and rights">
            {exciseId === null ? (
              <Typography variant="body2" color="text.secondary">
                Select a company and an excise to see their pages.
              </Typography>
            ) : pages.isPending ? (
              <LoadingOverlay />
            ) : pages.isError ? (
              <ErrorState error={pages.error} onRetry={() => void pages.refetch()} />
            ) : (
              <RolePagesPicker
                pages={visiblePages}
                selected={selected}
                baseline={NO_RIGHTS}
                canManageAdmins={canManageAdmins}
                disabled={create.isPending}
                onChange={setSelected}
              />
            )}
          </Section>

          <FormActions
            submitLabel={targets.length > 1 ? `Create ${targets.length} roles` : 'Create role'}
            onCancel={() => void navigate(PATHS.users.roles)}
            busy={create.isPending}
          />
        </form>
      </FormProvider>
    </>
  );
}

function OutcomeList({ outcomes, roleName }: { outcomes: readonly CreateRoleOutcome[]; roleName: string }) {
  const failed = outcomes.filter((o) => o.error !== null).length;
  return (
    <Alert severity={failed === outcomes.length ? 'error' : 'warning'} sx={{ mb: 2 }}>
      <AlertTitle>
        {failed === outcomes.length
          ? 'No role was created.'
          : `${outcomes.length - failed} of ${outcomes.length} roles saved.`}
      </AlertTitle>
      {outcomes.map((o) => (
        <Typography key={o.supplierCode.id} variant="body2">
          {`${roleName} - ${o.supplierCode.displayName}: `}
          {o.error === null
            ? 'created.'
            : o.role
              ? `created, but its rights could not be saved (${describeError(o.error)}).`
              : describeError(o.error)}
        </Typography>
      ))}
    </Alert>
  );
}
