import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { sellerApi } from '../api/seller'
import type { CreateProductInput, CreateVariantInput } from '../api/types'
import { CategorySelect } from '../components/CategorySelect'
import { errorLookup, Field, primaryButton, SelectField, secondaryButton, TextAreaField } from '../components/Field'
import { parsePrice } from '../lib/price'

const MAX_IMAGES = 8
const PRICE_ERROR = 'Fiyatı 1.249,90 biçiminde yazın (en çok 2 ondalık).'

interface VariantForm {
  key: number
  sku: string
  color: string
  size: string
  price: string
  listPrice: string
  vatRate: string
  stock: string
}

interface ImageForm {
  key: number
  url: string
}

const emptyVariant = (key: number): VariantForm => ({ key, sku: '', color: '', size: '', price: '', listPrice: '', vatRate: '20', stock: '0' })

// Satır içinde gösterilen alanlar; kalan sunucu hataları üstteki özette listelenir.
const KNOWN_ERROR = /^(name|description|categoryid|imageurls(\[\d+\])?|variants|variants\[\d+\]\.(sku|color|size|price|listprice|vatrate|stock))$/

export function ProductNewPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const nextKey = useRef(2)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [images, setImages] = useState<ImageForm[]>([{ key: 1, url: '' }])
  const [variants, setVariants] = useState<VariantForm[]>([emptyVariant(0)])
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const create = useMutation({
    mutationFn: sellerApi.createProduct,
    onSuccess: (product) => {
      void queryClient.invalidateQueries({ queryKey: ['seller', 'products'] })
      void queryClient.invalidateQueries({ queryKey: ['seller', 'dashboard'] })
      navigate(`/urunler/${product.id}`, { replace: true })
    },
  })

  const serverErrors = create.error instanceof ApiError ? create.error.fieldErrors : {}
  const errors = { ...serverErrors, ...clientErrors }
  const err = errorLookup(errors)
  const unmatched = Object.entries(errors).filter(([key]) => !KNOWN_ERROR.test(key.toLowerCase()))
  const generalError = create.error && Object.keys(serverErrors).length === 0 ? (create.error instanceof ApiError ? create.error.message : 'Ürün kaydedilemedi.') : null
  const hasErrors = Object.keys(errors).length > 0

  const setVariant = (key: number, field: keyof VariantForm, value: string) =>
    setVariants((list) => list.map((v) => (v.key === key ? { ...v, [field]: value } : v)))

  function buildPayload(submitForApproval: boolean): CreateProductInput | null {
    const found: Record<string, string> = {}
    const imageUrls = images.map((i) => i.url.trim()).filter(Boolean)
    images.forEach((image, index) => {
      const url = image.url.trim()
      if (url && !url.startsWith('https://')) found[`imageUrls[${index}]`] = 'Görsel adresi https:// ile başlamalı.'
    })

    const parsed: CreateVariantInput[] = []
    variants.forEach((v, i) => {
      const price = parsePrice(v.price)
      if (price === null) found[`variants[${i}].price`] = PRICE_ERROR
      const listPrice = v.listPrice.trim() ? parsePrice(v.listPrice) : null
      if (v.listPrice.trim() && listPrice === null) found[`variants[${i}].listPrice`] = PRICE_ERROR
      if (!/^\d+$/.test(v.stock.trim())) found[`variants[${i}].stock`] = 'Stok 0 ya da daha büyük bir tam sayı olmalı.'
      if (price !== null && !(v.listPrice.trim() && listPrice === null)) {
        parsed.push({
          sku: v.sku.trim(),
          barcode: null,
          color: v.color.trim() || null,
          size: v.size.trim() || null,
          price,
          listPrice,
          vatRate: Number(v.vatRate),
          stock: Number(v.stock.trim()) || 0,
        })
      }
    })

    setClientErrors(found)
    if (Object.keys(found).length > 0) return null
    return {
      name: name.trim(),
      description: description.trim() || null,
      categoryId: Number(categoryId) || 0,
      brandId: null,
      imageUrls,
      variants: parsed,
      submitForApproval,
    }
  }

  function save(submitForApproval: boolean) {
    if (create.isPending) return
    create.reset()
    const payload = buildPayload(submitForApproval)
    if (payload) create.mutate(payload)
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    save(false)
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-6" noValidate aria-busy={create.isPending}>
      <div>
        <Link to="/urunler" className="text-sm text-brand-700 hover:underline">← Ürünlere dön</Link>
        <h1 className="mt-1 text-xl font-bold">Yeni ürün</h1>
      </div>

      {(generalError || hasErrors) && (
        <div role="alert" className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800">
          <p className="font-semibold">{generalError ?? 'Formda düzeltilmesi gereken alanlar var.'}</p>
          {unmatched.length > 0 && (
            <ul className="mt-1 list-disc pl-5">
              {unmatched.map(([key, message]) => (
                <li key={key}>{message}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      <section aria-labelledby="info-title" className="flex flex-col gap-4 rounded-xl border border-line bg-white p-4 sm:p-5">
        <h2 id="info-title" className="font-bold">Ürün bilgileri</h2>
        <Field label="Ürün adı" name="name" value={name} onChange={(e) => setName(e.target.value)} error={err('name')} maxLength={200} hint="En az 5 karakter" />
        <TextAreaField label="Açıklama" name="description" rows={5} value={description} onChange={(e) => setDescription(e.target.value)} error={err('description')} maxLength={5000} />
        <CategorySelect value={categoryId} onChange={setCategoryId} error={err('categoryId')} />
      </section>

      <section aria-labelledby="images-title" className="flex flex-col gap-3 rounded-xl border border-line bg-white p-4 sm:p-5">
        <h2 id="images-title" className="font-bold">Görseller</h2>
        {err('imageUrls') && <p className="text-xs text-red-700">{err('imageUrls')}</p>}
        {images.map((image, index) => (
          <div key={image.key} className="flex items-end gap-2">
            <div className="min-w-0 flex-1">
              <Field
                label={`Görsel adresi ${index + 1}`}
                name={`image-${image.key}`}
                type="url"
                inputMode="url"
                placeholder="https://"
                value={image.url}
                onChange={(e) => setImages((list) => list.map((i) => (i.key === image.key ? { ...i, url: e.target.value } : i)))}
                error={err(`imageUrls[${index}]`)}
              />
            </div>
            <button
              type="button"
              onClick={() => setImages((list) => list.filter((i) => i.key !== image.key))}
              aria-label={`Görsel adresi ${index + 1} satırını sil`}
              className={`${secondaryButton} shrink-0`}
            >
              Sil
            </button>
          </div>
        ))}
        <div>
          <button
            type="button"
            disabled={images.length >= MAX_IMAGES}
            onClick={() => setImages((list) => [...list, { key: nextKey.current++, url: '' }])}
            className={secondaryButton}
          >
            Görsel ekle
          </button>
          <span className="ml-3 text-xs text-muted">En fazla {MAX_IMAGES} görsel, https adresi.</span>
        </div>
      </section>

      <section aria-labelledby="variants-title" className="flex flex-col gap-4 rounded-xl border border-line bg-white p-4 sm:p-5">
        <h2 id="variants-title" className="font-bold">Varyantlar</h2>
        {err('variants') && <p className="text-xs text-red-700">{err('variants')}</p>}
        {variants.map((v, i) => {
          const id = (field: string) => `variant-${v.key}-${field}`
          return (
            <fieldset key={v.key} className="min-w-0 rounded-lg border border-line p-3 sm:p-4">
              <legend className="px-1 text-sm font-semibold">Varyant {i + 1}</legend>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                <Field
                  id={id('sku')}
                  label="SKU"
                  name={id('sku')}
                  value={v.sku}
                  onChange={(e) => setVariant(v.key, 'sku', e.target.value.toUpperCase())}
                  error={err(`variants[${i}].sku`)}
                  hint="4-40 karakter: A-Z, 0-9, tire"
                  autoCapitalize="characters"
                />
                <Field id={id('color')} label="Renk" name={id('color')} value={v.color} onChange={(e) => setVariant(v.key, 'color', e.target.value)} error={err(`variants[${i}].color`)} />
                <Field id={id('size')} label="Beden" name={id('size')} value={v.size} onChange={(e) => setVariant(v.key, 'size', e.target.value)} error={err(`variants[${i}].size`)} />
                <Field id={id('stock')} label="Stok" name={id('stock')} inputMode="numeric" value={v.stock} onChange={(e) => setVariant(v.key, 'stock', e.target.value)} error={err(`variants[${i}].stock`)} />
                <Field id={id('price')} label="Fiyat (TL)" name={id('price')} type="text" inputMode="decimal" placeholder="249,90" value={v.price} onChange={(e) => setVariant(v.key, 'price', e.target.value)} error={err(`variants[${i}].price`)} />
                <Field id={id('listPrice')} label="Liste fiyatı (TL)" name={id('listPrice')} type="text" inputMode="decimal" placeholder="299,90" value={v.listPrice} onChange={(e) => setVariant(v.key, 'listPrice', e.target.value)} error={err(`variants[${i}].listPrice`)} hint="İsteğe bağlı" />
                <SelectField id={id('vat')} label="KDV oranı" name={id('vat')} value={v.vatRate} onChange={(e) => setVariant(v.key, 'vatRate', e.target.value)} error={err(`variants[${i}].vatRate`)}>
                  {[0, 1, 10, 20].map((rate) => (
                    <option key={rate} value={rate}>%{rate}</option>
                  ))}
                </SelectField>
              </div>
              {variants.length > 1 && (
                <button
                  type="button"
                  onClick={() => setVariants((list) => list.filter((x) => x.key !== v.key))}
                  className="mt-3 text-sm font-semibold text-red-700 hover:underline"
                >
                  Varyant {i + 1} satırını sil
                </button>
              )}
            </fieldset>
          )
        })}
        <div>
          <button
            type="button"
            disabled={variants.length >= 50}
            onClick={() => setVariants((list) => [...list, emptyVariant(nextKey.current++)])}
            className={secondaryButton}
          >
            Varyant ekle
          </button>
        </div>
      </section>

      <div className="flex flex-wrap gap-3">
        <button type="submit" disabled={create.isPending} className={secondaryButton}>Taslak kaydet</button>
        <button type="button" disabled={create.isPending} onClick={() => save(true)} className={primaryButton}>
          {create.isPending ? 'Kaydediliyor…' : 'Onaya gönder'}
        </button>
      </div>
    </form>
  )
}
