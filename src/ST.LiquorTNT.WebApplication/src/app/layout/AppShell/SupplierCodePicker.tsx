import ApartmentOutlined from '@mui/icons-material/ApartmentOutlined';
import ChevronRightRounded from '@mui/icons-material/ChevronRightRounded';
import SearchRounded from '@mui/icons-material/SearchRounded';
import StorefrontOutlined from '@mui/icons-material/StorefrontOutlined';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import Divider from '@mui/material/Divider';
import InputAdornment from '@mui/material/InputAdornment';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListSubheader from '@mui/material/ListSubheader';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useState } from 'react';

import type { SupplierCodeResponse } from '@/core/api';
import { useCurrentUser, useLogout } from '@/core/auth';

import { useSwitchSupplierCode } from './useSupplierCode';

const SEARCH_FROM = 6;

function franchiseOf(code: SupplierCodeResponse): string | null {
  const name = code.franchiseName?.trim();
  return name === undefined || name === '' ? null : name;
}

function groupByCompany(codes: readonly SupplierCodeResponse[]) {
  const groups = new Map<string, SupplierCodeResponse[]>();
  for (const code of [...codes].sort((a, b) => a.displayName.localeCompare(b.displayName))) {
    const company = code.companyName ?? `Company ${code.companyId}`;
    groups.set(company, [...(groups.get(company) ?? []), code]);
  }
  return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
}

function matches(code: SupplierCodeResponse, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  return [code.exciseCode, code.liquorCategoryCode, code.supplierCode, code.franchiseName, code.displayName]
    .filter((v): v is string => !!v)
    .some((v) => v.toLowerCase().includes(q));
}

export function SupplierCodePicker() {
  const user = useCurrentUser();
  const select = useSwitchSupplierCode();
  const logout = useLogout();
  const [query, setQuery] = useState('');

  const open = !user.isSuperAdmin && user.activeSupplierCode === null;
  const codes = user.supplierCodes;
  const groups = groupByCompany(codes.filter((c) => matches(c, query)));
  const showCompanyHeaders = new Set(codes.map((c) => c.companyId)).size > 1;
  const busyId = select.isPending ? select.variables : null;
  const name = user.fullName ?? user.userName;

  return (
    <Dialog
      open={open}
      aria-labelledby="supplier-code-title"
      aria-describedby="supplier-code-description"
      fullWidth
      maxWidth="sm"
      slotProps={{ paper: { sx: { borderRadius: 2, overflow: 'hidden' } } }}
    >
      <Box sx={{ px: 3, pt: 3, pb: 2.5, borderTop: 4, borderColor: 'error.main' }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Avatar sx={{ bgcolor: 'action.selected', color: 'text.primary', width: 48, height: 48 }}>
            <StorefrontOutlined />
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography id="supplier-code-title" component="h2" variant="h6">
              {codes.length > 0 ? 'Choose where to work' : 'No supplier code yet'}
            </Typography>
            <Typography id="supplier-code-description" variant="body2" color="text.secondary">
              {codes.length > 0
                ? `Welcome, ${name}. Pick a supplier code to continue. You can switch any time from the top bar.`
                : 'Your account is not assigned to any supplier code yet. Ask your administrator to give you a role.'}
            </Typography>
          </Box>
        </Stack>
        {codes.length >= SEARCH_FROM && (
          <TextField
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search by excise, category, supplier code or franchise"
            size="small"
            fullWidth
            autoFocus
            sx={{ mt: 2 }}
            slotProps={{
              htmlInput: { 'aria-label': 'Search supplier codes' },
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchRounded fontSize="small" />
                  </InputAdornment>
                ),
              },
            }}
          />
        )}
      </Box>

      {codes.length > 0 && (
        <>
          <Divider />
          <DialogContent sx={{ p: 0, maxHeight: 420 }}>
            {groups.length === 0 ? (
              <Typography variant="body2" color="text.secondary" sx={{ px: 3, py: 4, textAlign: 'center' }}>
                {`No supplier code matches "${query.trim()}".`}
              </Typography>
            ) : (
              <List disablePadding sx={{ py: 1 }}>
                {groups.map(([company, items]) => (
                  <li key={company}>
                    <List disablePadding>
                      {showCompanyHeaders && (
                        <ListSubheader
                          sx={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 1,
                            lineHeight: '36px',
                            px: 3,
                            bgcolor: 'background.default',
                            color: 'text.primary',
                            fontSize: 14,
                            fontWeight: 700,
                          }}
                        >
                          <ApartmentOutlined fontSize="small" />
                          {company}
                        </ListSubheader>
                      )}
                      {items.map((code) => {
                        const franchise = franchiseOf(code);
                        return (
                          <ListItemButton
                            key={code.id}
                            disabled={select.isPending}
                            onClick={() => select.mutate(code.id)}
                            aria-label={`Work in ${code.displayName}`}
                            sx={{ mx: 1.5, my: 0.5, px: 1.5, py: 1.25, borderRadius: 1.5, gap: 1.5 }}
                          >
                            <Avatar
                              variant="rounded"
                              sx={{
                                width: 40,
                                height: 40,
                                fontSize: 13,
                                fontWeight: 700,
                                bgcolor: 'primary.main',
                                color: 'primary.contrastText',
                              }}
                            >
                              {code.exciseCode}
                            </Avatar>
                            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                              <Typography variant="subtitle2" noWrap>
                                {`${code.exciseCode} ${code.liquorCategoryCode} ${code.supplierCode}`}
                              </Typography>
                              <Stack
                                direction="row"
                                spacing={0.75}
                                useFlexGap
                                sx={{ mt: 0.5, flexWrap: 'wrap', alignItems: 'center' }}
                              >
                                <Chip size="small" variant="outlined" label={`Category ${code.liquorCategoryCode}`} />
                                {franchise && (
                                  <Chip
                                    size="small"
                                    label={`Franchise · ${franchise}`}
                                    sx={{
                                      bgcolor: 'primary.main',
                                      color: 'primary.contrastText',
                                      fontWeight: 600,
                                      '& .MuiChip-icon': { color: 'inherit', fontSize: 16 },
                                    }}
                                  />
                                )}
                              </Stack>
                            </Box>
                            {busyId === code.id ? (
                              <CircularProgress size={20} aria-label="Opening" />
                            ) : (
                              <ChevronRightRounded color="action" />
                            )}
                          </ListItemButton>
                        );
                      })}
                    </List>
                  </li>
                ))}
              </List>
            )}
          </DialogContent>
        </>
      )}

      <Divider />
      <DialogActions sx={{ px: 3, py: 1.5, justifyContent: 'space-between' }}>
        <Typography variant="caption" color="text.secondary">
          {codes.length > 0 ? `${codes.length} supplier code${codes.length === 1 ? '' : 's'} available` : ''}
        </Typography>
        <Button onClick={() => logout.mutate()} disabled={logout.isPending}>
          Sign out
        </Button>
      </DialogActions>
    </Dialog>
  );
}
