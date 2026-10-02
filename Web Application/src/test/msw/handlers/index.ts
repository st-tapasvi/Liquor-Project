import { authHandlers } from './auth.handlers';
import { platformHandlers } from './platform.handlers';
import { settingsHandlers } from './settings.handlers';
import { usersHandlers } from './users.handlers';

export const handlers = [...authHandlers, ...platformHandlers, ...usersHandlers, ...settingsHandlers];
export { mockAuthState } from './auth.handlers';
export { mockUsers } from './users.handlers';
export { mockPlatformState } from './platform.handlers';
