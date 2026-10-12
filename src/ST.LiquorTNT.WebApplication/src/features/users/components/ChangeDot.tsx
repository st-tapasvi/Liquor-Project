import Box from '@mui/material/Box';
import Tooltip from '@mui/material/Tooltip';

export function ChangeDot({ label = 'Changed, not saved yet' }: { label?: string }) {
  return (
    <Tooltip title={label}>
      <Box
        component="span"
        role="img"
        aria-label={label}
        sx={{
          width: 8,
          height: 8,
          borderRadius: '50%',
          bgcolor: 'warning.main',
          flexShrink: 0,
          display: 'inline-block',
        }}
      />
    </Tooltip>
  );
}
