import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Panel İstanbul'daki çalışanlar içindir: testler de İstanbul saatinde çalışır (CI sunucusu UTC olsa bile).
process.env.TZ = 'Europe/Istanbul'

// Bölüm kontrol testleri (src/__bolum__). `npm run test:bolum -w admin` ile çalışır.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true,
  },
})
