import { createTheme } from '@mui/material/styles';

import { tokens } from './tokens';

export const theme = createTheme({
  cssVariables: true,
  palette: {
    mode: 'light',
    primary: { main: tokens.color.primary, dark: tokens.color.primaryDark },
    secondary: { main: tokens.color.secondary, dark: tokens.color.secondaryDark },
    success: { main: tokens.color.success },
    warning: { main: tokens.color.warning },
    error: { main: tokens.color.error },
    info: { main: tokens.color.info },
    background: { default: tokens.color.background, paper: tokens.color.surface },
    text: { primary: tokens.color.textPrimary, secondary: tokens.color.textSecondary },
    divider: tokens.color.border,
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
    MuiOutlinedInput: {
      styleOverrides: {
        root: { borderRadius: 4, backgroundColor: tokens.color.surface },
        notchedOutline: { borderColor: tokens.color.border },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: 4, textTransform: 'uppercase', fontWeight: 500, letterSpacing: '0.03em' },
        contained: { boxShadow: '0 1px 2px rgba(28, 28, 30, 0.2)' },
      },
    },
    MuiLink: {
      defaultProps: { underline: 'hover' },
      styleOverrides: {
        root: { color: tokens.color.accent, '&:hover': { color: tokens.color.accentDark } },
      },
    },
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: { root: { border: `1px solid ${tokens.color.border}` } },
    },
    MuiTableCell: { styleOverrides: { root: { paddingTop: 8, paddingBottom: 8 } } },
  },
});
