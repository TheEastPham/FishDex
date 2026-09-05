/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_URL: string;
  readonly VITE_AUTH_CLIENT_ID: string;
  readonly VITE_AUTH_REDIRECT_URI: string;
  readonly VITE_AUTH_POST_LOGOUT_URI: string;
  /** CARTO basemap key — lấy free tại https://carto.com/basemaps/apikey. Thiếu key thì map fallback sang OSM. */
  readonly VITE_CARTO_API_KEY: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
