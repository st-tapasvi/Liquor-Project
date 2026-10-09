import Box from '@mui/material/Box';
import Tab from '@mui/material/Tab';
import Tabs from '@mui/material/Tabs';
import type { ReactNode } from 'react';

export type UserTab = 'details' | 'roles';

const TABS: readonly { value: UserTab; label: string }[] = [
  { value: 'details', label: 'User Details' },
  { value: 'roles', label: 'Roles & Permissions' },
];

export function UserTabs({ value, onChange }: { value: UserTab; onChange: (tab: UserTab) => void }) {
  return (
    <Tabs
      value={value}
      onChange={(_event, tab: UserTab) => onChange(tab)}
      aria-label="User sections"
      sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}
    >
      {TABS.map((t) => (
        <Tab
          key={t.value}
          value={t.value}
          label={t.label}
          id={`user-tab-${t.value}`}
          aria-controls={`user-tabpanel-${t.value}`}
          sx={{ textTransform: 'none', fontWeight: 600 }}
        />
      ))}
    </Tabs>
  );
}

export function UserTabPanel({ tab, value, children }: { tab: UserTab; value: UserTab; children: ReactNode }) {
  return (
    <Box role="tabpanel" id={`user-tabpanel-${tab}`} aria-labelledby={`user-tab-${tab}`} hidden={tab !== value}>
      {children}
    </Box>
  );
}
