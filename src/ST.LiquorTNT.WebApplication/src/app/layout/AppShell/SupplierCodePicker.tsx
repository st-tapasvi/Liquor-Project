import ApartmentOutlined from '@mui/icons-material/ApartmentOutlined';
import ArrowForwardRounded from '@mui/icons-material/ArrowForwardRounded';
import LogoutRounded from '@mui/icons-material/LogoutRounded';
import SearchRounded from '@mui/icons-material/SearchRounded';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import ButtonBase from '@mui/material/ButtonBase';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import InputAdornment from '@mui/material/InputAdornment';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useState } from 'react';

import type { SupplierCodeResponse } from '@/core/api';
import { useCurrentUser, useLogout } from '@/core/auth';
import { appConfig } from '@/core/config';
import { tokens } from '@/core/theme';

import { useSwitchSupplierCode } from './useSupplierCode';

const SEARCH_FROM = 5;

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
  return [code.displayName, code.companyName, code.franchiseName, code.exciseCode, code.liquorCategoryCode]
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
  const busyId = select.isPending ? select.variables : null;
  const name = user.fullName ?? user.userName;

  return (
    <Dialog
      open={open}
      aria-labelledby="supplier-code-title"
      fullWidth
      maxWidth="md"
      slotProps={{ paper: { sx: { borderRadius: 1, overflow: 'hidden', maxHeight: 'min(640px, 90vh)' } } }}
    >
      <Box sx={{ display: 'flex', minHeight: 440, height: '100%' }}>
        <Box
          sx={{
            display: { xs: 'none', sm: 'flex' },
            flexDirection: 'column',
            width: 260,
            flexShrink: 0,
            p: 3,
            bgcolor: tokens.shell.sidebarBg,
            color: tokens.shell.sidebarActiveText,
            borderTop: `4px solid ${tokens.color.accent}`,
          }}
        >
          <Box component="img" src="/emblem.png" alt="" sx={{ height: 48, width: 'auto', alignSelf: 'flex-start' }} />
          <Typography variant="subtitle1" sx={{ mt: 2, fontWeight: 700 }}>
            {appConfig.name}
          </Typography>
          <Box sx={{ flexGrow: 1 }} />
          <Typography variant="overline" sx={{ color: tokens.shell.sidebarTextMuted }}>
            Signed in as
          </Typography>
          <Typography variant="subtitle2" noWrap>
            {name}
          </Typography>
          <Typography variant="caption" noWrap sx={{ color: tokens.shell.sidebarText }}>
            {user.userName}
          </Typography>
          <Button
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
            startIcon={<LogoutRounded fontSize="small" />}
            sx={{
              mt: 2,
              alignSelf: 'flex-start',
              color: tokens.shell.sidebarText,
              px: 0,
              '&:hover': { color: tokens.shell.sidebarActiveText, bgcolor: 'transparent' },
            }}
          >
            Sign out
          </Button>
        </Box>

        <Box sx={{ flexGrow: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ px: 3, pt: 3, pb: 2, borderBottom: `1px solid ${tokens.color.border}` }}>
            <Typography id="supplier-code-title" component="h2" variant="h5">
              {codes.length > 0 ? 'Select supplier code' : 'No supplier code assigned'}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {codes.length > 0
                ? 'Everything you see and do will be for this supplier code. You can switch later from the top bar.'
                : 'Your account has no role in any supplier code yet. Ask your administrator to assign one.'}
            </Typography>
            {codes.length >= SEARCH_FROM && (
              <TextField
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search supplier code, excise, category or company"
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

          <Box sx={{ flexGrow: 1, overflowY: 'auto', px: 3, py: 2.5, bgcolor: 'background.default' }}>
            {codes.length > 0 && groups.length === 0 && (
              <Typography variant="body2" color="text.secondary" sx={{ py: 6, textAlign: 'center' }}>
                {`No supplier code matches "${query.trim()}".`}
              </Typography>
            )}
            <Stack spacing={3}>
              {groups.map(([company, items]) => (
                <Box key={company} component="section" aria-label={company}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.25 }}>
                    <ApartmentOutlined fontSize="small" sx={{ color: 'text.secondary' }} />
                    <Typography variant="subtitle2">{company}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {`${items.length} code${items.length === 1 ? '' : 's'}`}
                    </Typography>
                  </Stack>
                  <Box
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                      gap: 1.5,
                    }}
                  >
                    {items.map((code) => (
                      <SupplierCodeTile
                        key={code.id}
                        code={code}
                        busy={busyId === code.id}
                        disabled={select.isPending}
                        onSelect={() => select.mutate(code.id)}
                      />
                    ))}
                  </Box>
                </Box>
              ))}
            </Stack>
          </Box>

          <Box
            sx={{
              display: { xs: 'flex', sm: 'none' },
              justifyContent: 'flex-end',
              px: 2,
              py: 1,
              borderTop: `1px solid ${tokens.color.border}`,
            }}
          >
            <Button onClick={() => logout.mutate()} disabled={logout.isPending} startIcon={<LogoutRounded />}>
              Sign out
            </Button>
          </Box>
        </Box>
      </Box>
    </Dialog>
  );
}

function SupplierCodeTile({
  code,
  busy,
  disabled,
  onSelect,
}: {
  code: SupplierCodeResponse;
  busy: boolean;
  disabled: boolean;
  onSelect: () => void;
}) {
  return (
    <ButtonBase
      onClick={onSelect}
      disabled={disabled}
      aria-label={`Work in ${code.displayName}`}
      sx={{
        display: 'flex',
        alignItems: 'stretch',
        textAlign: 'left',
        bgcolor: 'background.paper',
        border: `1px solid ${tokens.color.border}`,
        borderRadius: 1,
        overflow: 'hidden',
        transition: 'border-color 120ms, box-shadow 120ms',
        '&:hover, &.Mui-focusVisible': {
          borderColor: tokens.color.accent,
          boxShadow: '0 2px 8px rgba(28, 28, 30, 0.08)',
        },
        '&:hover .tile-arrow, &.Mui-focusVisible .tile-arrow': {
          color: tokens.color.accent,
          transform: 'translateX(2px)',
        },
        '&.Mui-disabled': { opacity: busy ? 1 : 0.55 },
      }}
    >
      <Box sx={{ width: 4, bgcolor: busy ? tokens.color.accent : tokens.color.head, flexShrink: 0 }} />
      <Box sx={{ flexGrow: 1, minWidth: 0, px: 2, py: 1.5 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Box
            component="span"
            sx={{
              px: 0.75,
              py: 0.25,
              borderRadius: 0.5,
              fontSize: tokens.font.caption,
              fontWeight: 700,
              letterSpacing: '0.04em',
              bgcolor: tokens.color.accentTint,
              color: tokens.color.accentDark,
            }}
          >
            {code.exciseCode}
          </Box>
          <Typography variant="caption" color="text.secondary">
            {code.liquorCategoryCode}
          </Typography>
        </Stack>
        <Typography variant="subtitle1" noWrap sx={{ mt: 0.75, fontWeight: 700 }}>
          {code.displayName}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap component="div">
          {code.franchiseName ?? `Supplier code ${code.supplierCode}`}
        </Typography>
      </Box>
      <Box sx={{ display: 'flex', alignItems: 'center', pr: 2 }}>
        {busy ? (
          <CircularProgress size={20} aria-label="Opening" />
        ) : (
          <ArrowForwardRounded
            className="tile-arrow"
            fontSize="small"
            sx={{ color: 'text.secondary', transition: 'transform 120ms, color 120ms' }}
          />
        )}
      </Box>
    </ButtonBase>
  );
}
