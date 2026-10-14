import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Bölüm kontrol testleri (src/__bolum__). `npm run test:bolum -w <uygulama>` ile çalışır.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true,
  },
})
