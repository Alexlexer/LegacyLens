import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: '../src/LegacyLens.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': 'http://127.0.0.1:5000',
      '/health': 'http://127.0.0.1:5000',
    },
  },
});
