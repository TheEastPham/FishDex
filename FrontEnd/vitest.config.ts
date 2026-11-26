import path from 'path';
import { defineConfig } from 'vitest/config';

// Cấu hình test riêng, không dùng lại vite.config của app: plugin Cloudflare chỉ dành cho build/deploy.
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'apps/aquahome-web/src'),
      '@fishlover/shared': path.resolve(__dirname, 'packages/shared/src'),
    },
  },
  test: {
    include: ['apps/*/src/**/*.test.{ts,tsx}', 'packages/*/src/**/*.test.{ts,tsx}'],
    // Mặc định Node; file test component tự khai `// @vitest-environment jsdom`.
    environment: 'node',
    env: { VITE_GATEWAY_URL: 'http://gateway.test' },
    restoreMocks: true,
  },
});
