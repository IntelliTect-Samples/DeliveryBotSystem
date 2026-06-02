import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: '/',
  server: {
    proxy: {
      '/api/simulator': {
        target: 'http://localhost:5099',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/simulator/, '')
      },
      '/api/order-service': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/order-service/, '')
      }
    }
  }
})
