import { createTheme } from '@mui/material/styles';

import { tokens } from './tokens';

/**
 * The single MUI theme. Dense defaults suit a data-heavy back-office application used on
 * desktop screens in a plant.
 */
export const theme = createTheme({
  cssVariables: true,
  palette: {
    mode: 'light',
    primary: { main: tokens.color.primary, dark: tokens.color.primaryDark },
    secondary: { main: tokens.color.secondary },
    success: { main: tokens.color.success },
    warning: { main: tokens.color.warning },
    error: { main: tokens.color.error },
    info: { main: tokens.color.info },
    background: { default: tokens.color.background, paper: tokens.color.surface },
    text: { primary: tokens.color.textPrimary, secondary: tokens.color.textSecondary },
  },
  shape: { borderRadius: tokens.radius },
  typography: {
    fontFamily: tokens.fontFamily,
    fontSize: 14,
    h4: { fontWeight: 600 },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
  },
  components: {
    MuiTextField: { defaultProps: { size: 'small', fullWidth: true, variant: 'outlined' } },
    MuiFormControl: { defaultProps: { size: 'small' } },
    MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { textTransform: 'none' } } },
    MuiPaper: { defaultProps: { elevation: 0 }, styleOverrides: { root: { border: '1px solid rgba(0,0,0,0.08)' } } },
    MuiTableCell: { styleOverrides: { root: { paddingTop: 8, paddingBottom: 8 } } },
  },
});
