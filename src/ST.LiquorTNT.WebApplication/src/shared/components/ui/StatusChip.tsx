import Chip, { type ChipProps } from '@mui/material/Chip';

export type StatusTone = 'success' | 'warning' | 'error' | 'info' | 'default';

interface StatusChipProps {
  label: string;
  tone: StatusTone;
  size?: ChipProps['size'];
}

/** Small coloured label for a state (Active, Locked, Inactive…). */
export function StatusChip({ label, tone, size = 'small' }: StatusChipProps) {
  return (
    <Chip
      label={label}
      size={size}
      color={tone === 'default' ? 'default' : tone}
      variant={tone === 'default' ? 'outlined' : 'filled'}
    />
  );
}
