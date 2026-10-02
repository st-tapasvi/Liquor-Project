/** Raw design tokens. Components never use these directly; they go through the MUI theme. */
export const tokens = {
  color: {
    primary: '#1b4965',
    primaryDark: '#12344a',
    secondary: '#5fa8d3',
    success: '#2e7d32',
    warning: '#ed6c02',
    error: '#c62828',
    info: '#0277bd',
    background: '#f4f6f8',
    surface: '#ffffff',
    textPrimary: '#1c2430',
    textSecondary: '#5a6472',
  },
  radius: 6,
  fontFamily: ['"Segoe UI"', 'Roboto', 'Helvetica', 'Arial', 'sans-serif'].join(','),
} as const;
