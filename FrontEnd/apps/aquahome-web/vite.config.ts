import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { execSync } from 'child_process';

import { cloudflare } from "@cloudflare/vite-plugin";

const commitHash = (() => {
  try { return execSync('git rev-parse --short HEAD').toString().trim(); }
  catch { return 'dev'; }
})();

export default defineConfig({
  plugins: [react(), cloudflare()],
  define: {
    'import.meta.env.VITE_APP_VERSION': JSON.stringify(commitHash),
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@fishlover/shared': path.resolve(__dirname, '../../packages/shared/src'),
    },
  },
  server: {
    port: 5173,
  },
});