/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// SPA del SIGH (research D9). La URL de la API llega por VITE_API_URL en tiempo de build.
export default defineConfig({
  plugins: [react()],
  server: { port: 5173 },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/pruebas/configuracion.ts'],
    // Cobertura (T116): npm run test:cobertura → coverage/index.html
    coverage: {
      provider: 'v8',
      reporter: ['text', 'text-summary', 'html', 'lcov', 'json-summary'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.test.tsx', 'src/pruebas/**', 'src/main.tsx', 'src/vite-env.d.ts'],
    },
  },
});
