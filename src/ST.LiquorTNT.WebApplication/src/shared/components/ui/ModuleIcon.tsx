import ApartmentOutlined from '@mui/icons-material/ApartmentOutlined';
import BarChartOutlined from '@mui/icons-material/BarChartOutlined';
import CalendarTodayOutlined from '@mui/icons-material/CalendarTodayOutlined';
import CodeOutlined from '@mui/icons-material/CodeOutlined';
import DescriptionOutlined from '@mui/icons-material/DescriptionOutlined';
import GridViewOutlined from '@mui/icons-material/GridViewOutlined';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import LayersOutlined from '@mui/icons-material/LayersOutlined';
import LocalOfferOutlined from '@mui/icons-material/LocalOfferOutlined';
import LocalShippingOutlined from '@mui/icons-material/LocalShippingOutlined';
import LockOutlined from '@mui/icons-material/LockOutlined';
import OutboxOutlined from '@mui/icons-material/OutboxOutlined';
import PeopleOutlined from '@mui/icons-material/PeopleOutlined';
import SettingsOutlined from '@mui/icons-material/SettingsOutlined';
import SyncOutlined from '@mui/icons-material/SyncOutlined';
import ViewInArOutlined from '@mui/icons-material/ViewInArOutlined';
import type { SvgIconProps } from '@mui/material/SvgIcon';
import type { ComponentType } from 'react';

import type { ModuleKey } from '@/core/modules';

/** One icon per module, shared by the sidebar and the dashboard tiles so the two always match. */
const ICONS: Record<ModuleKey, ComponentType<SvgIconProps>> = {
  auth: LockOutlined,
  dashboard: GridViewOutlined,
  company: ApartmentOutlined,
  plant: ViewInArOutlined,
  users: PeopleOutlined,
  brands: LocalOfferOutlined,
  batches: LayersOutlined,
  plans: CalendarTodayOutlined,
  'code-pool': CodeOutlined,
  palette: ViewInArOutlined,
  'case-data': DescriptionOutlined,
  dispatch: LocalShippingOutlined,
  'portal-sync': SyncOutlined,
  outbox: OutboxOutlined,
  reports: BarChartOutlined,
  settings: SettingsOutlined,
  license: InfoOutlined,
};

export function ModuleIcon({ module, ...props }: { module: ModuleKey } & SvgIconProps) {
  const Icon = ICONS[module];
  return <Icon {...props} />;
}
