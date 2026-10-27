import {defineConfig} from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    dedupe: ['@optimajet/workflow-designer']
  },
  server: {
    port: 5142,
  },
  build: {
    sourcemap: true
  }
})
