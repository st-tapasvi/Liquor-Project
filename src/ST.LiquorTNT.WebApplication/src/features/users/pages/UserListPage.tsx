import AddIcon from '@mui/icons-material/Add';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router';

import { Can } from '@/core/auth';
import { PATHS } from '@/core/router';

import { useServerGrid } from '@/shared/components/data-grid';
import { PageHeader } from '@/shared/components/ui';
import { useDebounce } from '@/shared/hooks';

import { UserGrid } from '../components/UserGrid';

export default function UserListPage() {
  const grid = useServerGrid();
  const [searchText, setSearchText] = useState(grid.state.search);
  const debounced = useDebounce(searchText, 300);

  useEffect(() => {
    if (debounced !== grid.state.search) grid.setSearch(debounced);
  }, [debounced]);

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Accounts that can sign in to this installation."
        actions={
          <Can right="user.add">
            <Button component={RouterLink} to={PATHS.users.new} variant="contained" startIcon={<AddIcon />}>
              New user
            </Button>
          </Can>
        }
      />
      <Box sx={{ mb: 2 }}>
        <TextField
          label="Search"
          placeholder="User name or full name"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          sx={{ width: '100%', maxWidth: 360, bgcolor: 'background.paper' }}
          slotProps={{ htmlInput: { maxLength: 100, 'aria-label': 'Search users' } }}
        />
      </Box>
      <UserGrid grid={grid} />
    </>
  );
}
