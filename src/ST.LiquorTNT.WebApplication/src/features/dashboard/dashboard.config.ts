import ApartmentOutlined from '@mui/icons-material/ApartmentOutlined';
import PeopleOutlined from '@mui/icons-material/PeopleOutlined';
import SettingsOutlined from '@mui/icons-material/SettingsOutlined';
import type { SvgIconProps } from '@mui/material/SvgIcon';
import type { ComponentType } from 'react';

import type { PermissionKey } from '@/core/auth';
import { PATHS } from '@/core/router';

export interface ModuleTileDef {
  key: string;
  label: string;
  to: string;
  icon: ComponentType<SvgIconProps>;
  permission: readonly PermissionKey[];
  pages: readonly string[];
}

export const MODULE_TILES: readonly ModuleTileDef[] = [
  {
    key: 'company',
    label: 'Company',
    to: PATHS.company.supplierCodes,
    icon: ApartmentOutlined,
    permission: ['company.view', 'suppliercode.view', 'liquorcategory.view'],
    pages: ['company', 'suppliercode', 'liquorcategory'],
  },
  {
    key: 'users',
    label: 'Users',
    to: PATHS.users.list,
    icon: PeopleOutlined,
    permission: ['user.view', 'role.view'],
    pages: ['user', 'role'],
  },
  {
    key: 'settings',
    label: 'Settings',
    to: PATHS.settings.security,
    icon: SettingsOutlined,
    permission: ['securityconfig.view', 'passwordpolicy.view'],
    pages: ['securityconfig', 'passwordpolicy'],
  },
];

export function rightsLabel(pages: readonly string[], rights: ReadonlySet<PermissionKey>): string {
  const actions = new Set<string>();
  for (const right of rights) {
    const dot = right.lastIndexOf('.');
    if (pages.includes(right.slice(0, dot))) actions.add(right.slice(dot + 1));
  }
  const list = [...actions];
  return list.length > 4 ? `${list.slice(0, 3).join(' · ')} +${list.length - 3}` : list.join(' · ');
}
