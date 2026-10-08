import type { UserResponse } from '@/core/api';

import type { StatusTone } from '@/shared/components/ui';

export function userStatus(user: UserResponse): { label: string; tone: StatusTone } {
  if (!user.isActive) return { label: 'Inactive', tone: 'default' };
  if (user.isBlocked) return { label: 'Blocked', tone: 'error' };
  if (user.lockedUntil) return { label: 'Locked', tone: 'warning' };
  if (user.forcePasswordChange) return { label: 'Password change pending', tone: 'info' };
  return { label: 'Active', tone: 'success' };
}
