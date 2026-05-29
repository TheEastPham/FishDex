/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_AQUAHOME_API_URL: string;
  readonly VITE_FISHDEX_API_URL: string;
  readonly VITE_AUTH_ISSUER: string;
  readonly VITE_AUTH_CLIENT_ID: string;
  readonly VITE_AUTH_REDIRECT_URI: string;
  readonly VITE_AUTH_POST_LOGOUT_URI: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
