import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemText from '@mui/material/ListItemText';

import { useCurrentUser, useLogout } from '@/core/auth';

import { useSwitchSupplierCode } from './useSupplierCode';

export function SupplierCodePicker() {
  const user = useCurrentUser();
  const select = useSwitchSupplierCode();
  const logout = useLogout();

  const open = !user.isSuperAdmin && user.activeSupplierCode === null;
  const codes = user.supplierCodes;

  return (
    <Dialog open={open} aria-labelledby="supplier-code-title" fullWidth maxWidth="xs">
      <DialogTitle id="supplier-code-title">
        {codes.length > 0 ? 'Select a supplier code' : 'No supplier code'}
      </DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 1 }}>
          {codes.length > 0
            ? 'Choose where you want to work. You can switch later from the top bar.'
            : 'Your account is not assigned to any supplier code yet. Ask your administrator to give you a role.'}
        </DialogContentText>
        {codes.length > 0 && (
          <List disablePadding>
            {codes.map((code) => (
              <ListItemButton
                key={code.id}
                disabled={select.isPending}
                onClick={() => select.mutate(code.id)}
                sx={{ borderRadius: 1 }}
              >
                <ListItemText primary={code.displayName} secondary={code.companyName ?? undefined} />
              </ListItemButton>
            ))}
          </List>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={() => logout.mutate()} disabled={logout.isPending}>
          Sign out
        </Button>
      </DialogActions>
    </Dialog>
  );
}
