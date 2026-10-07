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
  },
  shape: { borderRadius: tokens.radius },
  typography: {
    fontFamily: tokens.fontFamily,
    fontSize: 14,
    h3: { fontSize: tokens.font.figure, fontWeight: 700, lineHeight: 1.2 },
    h4: { fontSize: tokens.font.pageTitle, fontWeight: 700, lineHeight: 1.25 },
    h5: { fontSize: tokens.font.cardTitle, fontWeight: 700, lineHeight: 1.3 },
    h6: { fontSize: tokens.font.sectionTitle, fontWeight: 700, lineHeight: 1.35 },
    subtitle1: { fontSize: tokens.font.subtitle, fontWeight: 600, lineHeight: 1.4 },
    subtitle2: { fontSize: tokens.font.body1, fontWeight: 600, lineHeight: 1.45 },
    body1: { fontSize: tokens.font.body1, lineHeight: 1.5 },
    body2: { fontSize: tokens.font.body2, lineHeight: 1.5 },
    caption: { fontSize: tokens.font.caption, lineHeight: 1.45 },
    overline: { fontSize: tokens.font.overline, letterSpacing: '0.06em', lineHeight: 1.5 },
    button: { fontSize: tokens.font.body2, fontWeight: 500 },
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
      styleOverrides: { elevation0: { border: `1px solid ${tokens.color.border}` } },
    },
    MuiTooltip: { styleOverrides: { tooltip: { fontSize: tokens.font.caption } } },
    MuiChip: { styleOverrides: { label: { fontSize: tokens.font.caption } } },
    MuiFormHelperText: { styleOverrides: { root: { fontSize: tokens.font.caption } } },
  },
});
