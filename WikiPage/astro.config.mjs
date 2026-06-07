import { defineConfig } from 'astro/config';

export default defineConfig({
  output: 'static',
  // Update `site` before deploying to Azure Static Web Apps
  site: 'https://fishdex.example.com',
});
