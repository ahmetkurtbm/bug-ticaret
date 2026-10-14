// Bölüm API-01 kontrolü: sunucunun döndürdüğü alan hataları ilgili alanın altında görünmeli.
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { expect, test, vi } from 'vitest'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import { AuthProvider } from '../auth/AuthContext'
import { ApplicationPage } from '../pages/ApplicationPage'

test('400 yanıtındaki IBAN ve vergi no hataları alanların altında gösterilir', async () => {
  vi.spyOn(sellerApi, 'myApplication').mockResolvedValue(null)
  vi.spyOn(sellerApi, 'apply').mockRejectedValue(
    new ApiError(400, {
      status: 400,
      title: 'Girilen bilgileri kontrol edin.',
      errors: { Iban: ['IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin.'], TaxNumber: ['Bu vergi numarasıyla kayıtlı bir mağaza var.'] },
    }),
  )

  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AuthProvider>
          <ApplicationPage />
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )

  const fill = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })
  await screen.findByLabelText('IBAN')
  fill('Mağaza adı', 'Deneme Mağaza')
  fill('Ticari unvan / ad soyad', 'Deneme Ltd')
  fill('Vergi no / T.C. kimlik no', '1234567890')
  fill('Vergi dairesi', 'Kadıköy')
  fill('IBAN', 'TR00 0000')
  fill('Şehir', 'İstanbul')
  fireEvent.click(screen.getByRole('button', { name: 'Başvuruyu gönder' }))

  expect(await screen.findByText('IBAN geçersiz. TR ile başlayan 26 karakterli IBAN girin.')).toBeTruthy()
  expect(screen.getByText('Bu vergi numarasıyla kayıtlı bir mağaza var.')).toBeTruthy()
})
