import ApartmentOutlined from '@mui/icons-material/ApartmentOutlined';
import GridViewOutlined from '@mui/icons-material/GridViewOutlined';
import PeopleOutlined from '@mui/icons-material/PeopleOutlined';
import SettingsOutlined from '@mui/icons-material/SettingsOutlined';
import type { SvgIconProps } from '@mui/material/SvgIcon';
import type { ComponentType } from 'react';

import type { PermissionKey } from '@/core/auth';
import { PATHS } from '@/core/router';

export interface MenuLink {
  label: string;
  to: string;
  permission?: readonly PermissionKey[];
}

export interface MenuItem extends MenuLink {
  icon: ComponentType<SvgIconProps>;
  children?: readonly MenuLink[];
}

export const MENU: readonly MenuItem[] = [
  { label: 'Dashboard', to: PATHS.dashboard, icon: GridViewOutlined },
  {
    label: 'Company',
    to: PATHS.company.list,
    icon: ApartmentOutlined,
    children: [
      { label: 'Companies', to: PATHS.company.list, permission: ['company.view'] },
      { label: 'Supplier codes', to: PATHS.company.supplierCodes, permission: ['suppliercode.view'] },
      { label: 'Liquor categories', to: PATHS.company.liquorCategories, permission: ['liquorcategory.view'] },
    ],
  },
  {
    label: 'Users',
    to: PATHS.users.list,
    icon: PeopleOutlined,
    children: [
      { label: 'Users', to: PATHS.users.list, permission: ['user.view'] },
      { label: 'Roles', to: PATHS.users.roles, permission: ['role.view'] },
    ],
  },
  {
    label: 'Settings',
    to: PATHS.settings.security,
    icon: SettingsOutlined,
    children: [
      { label: 'Security settings', to: PATHS.settings.security, permission: ['securityconfig.view'] },
      { label: 'Password policies', to: PATHS.settings.passwordPolicies, permission: ['passwordpolicy.view'] },
      { label: 'My sessions', to: PATHS.settings.sessions },
    ],
  },
];
