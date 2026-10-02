/// <reference types="vite/client" />

/** Injected at build time from package.json (see vite.config.ts `define`). */
declare const __APP_VERSION__: string;
/** ISO timestamp of the build, injected by vite.config.ts. */
declare const __BUILD_TIME__: string;

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_LOG_LEVEL?: string;
  readonly VITE_APP_NAME?: string;
}
