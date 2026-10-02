import AddIcon from '@mui/icons-material/Add';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router';

import { Can } from '@/core/auth';
import { PATHS } from '@/core/router';

import { useServerGrid } from '@/shared/components/data-grid';
import { Section } from '@/shared/components/layout';
import { PageHeader } from '@/shared/components/ui';
import { useDebounce } from '@/shared/hooks';

import { UserGrid } from '../components/UserGrid';

export default function UserListPage() {
  const grid = useServerGrid();
  const [searchText, setSearchText] = useState(grid.state.search);
  const debounced = useDebounce(searchText, 300);

  // Why an effect: the debounced value is pushed into the URL (external state) after the user pauses typing.
  useEffect(() => {
    if (debounced !== grid.state.search) grid.setSearch(debounced);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- grid.setSearch is stable per render of the state; re-running on state.search would loop.
  }, [debounced]);

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Accounts that can sign in to this installation."
        actions={
          <Can right="users.manage">
            <Button component={RouterLink} to={PATHS.users.new} variant="contained" startIcon={<AddIcon />}>
              New user
            </Button>
          </Can>
        }
      />
      <Section dense>
        <TextField
          label="Search"
          placeholder="User name or full name"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          sx={{ maxWidth: 360, mb: 1 }}
          slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Search users' } }}
        />
        <UserGrid grid={grid} />
      </Section>
    </>
  );
}
