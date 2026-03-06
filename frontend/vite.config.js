import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 4101,
    proxy: {
      '/api': 'http://localhost:5118',
      '/auth': 'http://localhost:5118',
      '/login': 'http://localhost:5118',
      '/logout': 'http://localhost:5118',
      '/signin-oidc': 'http://localhost:5118',
      '/signout-callback-oidc': 'http://localhost:5118',
    },
  },
});
