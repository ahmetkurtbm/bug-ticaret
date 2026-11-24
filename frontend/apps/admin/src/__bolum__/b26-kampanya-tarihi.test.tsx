// Bölüm 26 kontrolü: kupon formunda seçilen günler API'ye aynen gitmeli.
import { fireEvent, screen, waitFor } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { adminApi } from '../api/admin'
import type { AdminCoupon } from '../api/adminTypes'
import { CampaignsPage } from '../pages/CampaignsPage'
import { renderAt } from './test-utils'

test('kupon formunda seçilen başlangıç ve bitiş günü değişmeden kaydedilir', async () => {
  vi.spyOn(adminApi, 'coupons').mockResolvedValue([])
  vi.spyOn(adminApi, 'campaignHistory').mockResolvedValue([])
  const create = vi.spyOn(adminApi, 'createCoupon').mockResolvedValue({ id: 1 } as AdminCoupon)

  renderAt('/kampanyalar', '/kampanyalar', <CampaignsPage />)
  fireEvent.click(await screen.findByRole('button', { name: 'Yeni kupon' }))
  const fill = (label: string, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })
  fill('Kod', 'ARALIK10')
  fill('Açıklama', 'Aralık indirimi')
  fill('İndirim oranı (%)', '10')
  fill('Başlangıç günü', '2026-12-01')
  fill('Bitiş günü (dahil)', '2026-12-31')
  fireEvent.click(screen.getByRole('button', { name: 'Kuponu oluştur' }))

  await waitFor(() => expect(create).toHaveBeenCalled())
  expect(create.mock.calls[0][0]).toMatchObject({ startsOn: '2026-12-01', endsOn: '2026-12-31' })
})
