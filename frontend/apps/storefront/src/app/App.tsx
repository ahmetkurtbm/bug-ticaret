import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { BrowserRouter, Route, Routes } from 'react-router'
import { ApiError } from '../api/client'
import { AuthProvider } from '../auth/AuthContext'
import { Layout } from '../components/Layout'
import { RequireAuth } from '../components/RequireAuth'
import { CartPage } from '../pages/CartPage'
import { CheckoutPage } from '../pages/CheckoutPage'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { OrderDetailPage } from '../pages/OrderDetailPage'
import { OrderResultPage } from '../pages/OrderResultPage'
import { OrdersPage } from '../pages/OrdersPage'
import { ProductListPage } from '../pages/ProductListPage'
import { ProductPage } from '../pages/ProductPage'
import { RegisterPage } from '../pages/RegisterPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 4xx yanıtları tekrar denemenin anlamı yok; ağ hatası ve 5xx için 2 kez dene.
      retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 2,
      refetchOnWindowFocus: false,
    },
  },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<HomePage />} />
              <Route path="kategori/:slug" element={<ProductListPage />} />
              <Route path="ara" element={<ProductListPage />} />
              <Route path="urun/:slug" element={<ProductPage />} />
              <Route path="sepet" element={<CartPage />} />
              <Route path="odeme" element={<RequireAuth><CheckoutPage /></RequireAuth>} />
              <Route path="siparis/:orderNumber/sonuc" element={<RequireAuth><OrderResultPage /></RequireAuth>} />
              <Route path="hesabim/siparisler" element={<RequireAuth><OrdersPage /></RequireAuth>} />
              <Route path="hesabim/siparisler/:orderNumber" element={<RequireAuth><OrderDetailPage /></RequireAuth>} />
              <Route path="giris" element={<LoginPage />} />
              <Route path="kayit" element={<RegisterPage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}
