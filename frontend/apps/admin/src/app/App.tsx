import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router'
import { ApiError } from '../api/client'
import { AuthProvider } from '../auth/AuthContext'
import { Layout } from '../components/Layout'
import { RequireAdmin } from '../components/RequireAdmin'
import { CampaignsPage } from '../pages/CampaignsPage'
import { DashboardPage } from '../pages/DashboardPage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { OrderDetailPage } from '../pages/OrderDetailPage'
import { OrdersPage } from '../pages/OrdersPage'
import { ProductApprovalPage } from '../pages/ProductApprovalPage'
import { SellersPage } from '../pages/SellersPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 4xx yanıtlarını tekrar denemenin anlamı yok; ağ hatası ve 5xx için 2 kez dene.
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
            <Route element={<RequireAdmin><Layout /></RequireAdmin>}>
              <Route index element={<DashboardPage />} />
              <Route path="siparisler" element={<OrdersPage />} />
              <Route path="siparisler/:orderNumber" element={<OrderDetailPage />} />
              <Route path="saticilar" element={<SellersPage />} />
              <Route path="urun-onayi" element={<ProductApprovalPage />} />
              <Route path="kampanyalar" element={<CampaignsPage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}
