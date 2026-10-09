import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import Button from '@mui/material/Button';
import { Link as RouterLink, useMatch, useNavigate, useParams } from 'react-router';

import { Can, usePermission } from '@/core/auth';
import { PATHS } from '@/core/router';

import { ErrorState, LoadingOverlay } from '@/shared/components/feedback';
import { Section } from '@/shared/components/layout';
import { PageHeader, StatusChip } from '@/shared/components/ui';

import { useCreateUser, useUpdateUser } from '../api/users.mutations';
import { useUser } from '../api/users.queries';
import { userStatus } from '../components/user-status';
import { UserForm } from '../components/UserForm';
import { UserRolesSection } from '../components/UserRolesSection';
import { fromUser, toCreateRequest, toUpdateRequest } from '../users.schema';

/** Serves /users/new, /users/:id (read-only view) and /users/:id/edit. */
export default function UserEditPage() {
  const { id } = useParams<{ id: string }>();
  const userId = id === undefined ? undefined : /^\d+$/.test(id) ? Number(id) : Number.NaN;
  const navigate = useNavigate();
  const isEditRoute = useMatch(PATHS.users.edit()) !== null;
  const canEdit = usePermission('user.edit');

  if (userId === undefined) return <CreateView onDone={() => navigate(PATHS.users.list)} />;
  if (!Number.isInteger(userId) || userId <= 0) return <ErrorState error={new Error('Invalid user id.')} />;
  return <EditView userId={userId} readOnly={!isEditRoute || !canEdit} onDone={() => navigate(PATHS.users.list)} />;
}

function CreateView({ onDone }: { onDone: () => void }) {
  const create = useCreateUser();
  return (
    <>
      <PageHeader title="New user" />
      <Section>
        <UserForm
          mode="create"
          busy={create.isPending}
          onCancel={onDone}
          onSubmit={async (values) => {
            await create.mutateAsync(toCreateRequest(values));
            onDone();
          }}
        />
      </Section>
    </>
  );
}

function EditView({ userId, readOnly, onDone }: { userId: number; readOnly: boolean; onDone: () => void }) {
  const user = useUser(userId);
  const update = useUpdateUser(userId);

  if (user.isPending) return <LoadingOverlay />;
  if (user.isError) return <ErrorState error={user.error} onRetry={() => void user.refetch()} />;

  const status = userStatus(user.data);

  return (
    <>
      <PageHeader
        title={user.data.userName}
        subtitle={user.data.fullName ?? undefined}
        actions={
          <>
            <StatusChip label={status.label} tone={status.tone} />
            {readOnly && (
              <>
                <Button component={RouterLink} to={PATHS.users.list} startIcon={<ArrowBackIcon />}>
                  Back
                </Button>
                <Can right="user.edit">
                  <Button
                    component={RouterLink}
                    to={PATHS.users.edit(userId)}
                    variant="contained"
                    startIcon={<EditOutlinedIcon />}
                  >
                    Edit
                  </Button>
                </Can>
              </>
            )}
          </>
        }
      />
      <Section>
        <UserForm
          mode="edit"
          key={user.data.id}
          readOnly={readOnly}
          defaultValues={fromUser(user.data)}
          busy={update.isPending}
          onCancel={onDone}
          onSubmit={async (values) => {
            await update.mutateAsync(toUpdateRequest(values));
            onDone();
          }}
        />
      </Section>
      <UserRolesSection userId={userId} />
    </>
  );
}
