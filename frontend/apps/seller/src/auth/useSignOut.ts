import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { useAuth } from './AuthContext'

/** Çıkış yapar, önbelleği temizler ve giriş sayfasına götürür (önceki kullanıcının verisi kalmasın). */
export function useSignOut() {
  const { logout } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  return async () => {
    navigate('/giris', { replace: true })
    await logout()
    queryClient.clear()
  }
}
