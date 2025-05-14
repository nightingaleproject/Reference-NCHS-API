import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  base: '',
  build: {
    outDir: '../status_api/StatusUI',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api/v1/status': {
        target: 'https://localhost:5001',
        secure: false,
        changeOrigin: true,
      },
    },
  },
})
