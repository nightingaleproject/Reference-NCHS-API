import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  base: '',
  build: {
    outDir: '../messaging/StatusUI',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/status': {
        target: 'https://localhost:5001',
        secure: false,
        changeOrigin: true,
      },
    },
  },
})
