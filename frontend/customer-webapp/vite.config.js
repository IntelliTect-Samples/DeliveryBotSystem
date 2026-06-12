import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  base: '/',
  server: {
    proxy: {
      '/api/orders': {
        target: 'http://localhost:5180',
        changeOrigin: true,
      },
      '/api/agent': {
        target: 'http://localhost:7071',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/agent/, ''),
      },
      '/api/simulator': {
        target: 'http://localhost:5099',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/simulator/, '')
      }
    }
  }
})
