import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { BrowserRouter, Route, Routes } from 'react-router'
import { ApiError } from '../api/client'
import { AuthProvider } from '../auth/AuthContext'
import { RequireAuth } from '../components/RequireAuth'
import { SellerLayout } from '../components/SellerLayout'
import { ApplicationPage } from '../pages/ApplicationPage'
import { DashboardPage } from '../pages/DashboardPage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { OrdersPage } from '../pages/OrdersPage'
import { ProductDetailPage } from '../pages/ProductDetailPage'
import { ProductListPage } from '../pages/ProductListPage'
import { ProductNewPage } from '../pages/ProductNewPage'
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
            <Route path="giris" element={<LoginPage />} />
            <Route path="kayit" element={<RegisterPage />} />
            <Route path="basvuru" element={<RequireAuth><ApplicationPage /></RequireAuth>} />
            <Route element={<SellerLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="urunler" element={<ProductListPage />} />
              <Route path="urunler/yeni" element={<ProductNewPage />} />
              <Route path="urunler/:id" element={<ProductDetailPage />} />
              <Route path="siparisler" element={<OrdersPage />} />
            </Route>
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}
