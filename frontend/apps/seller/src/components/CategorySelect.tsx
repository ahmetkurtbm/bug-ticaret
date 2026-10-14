import { useQuery } from '@tanstack/react-query'
import { sellerApi } from '../api/seller'
import type { CategoryNode } from '../api/types'
import { SelectField } from './Field'

interface Group {
  label: string
  leaves: CategoryNode[]
}

/** Ağacı yaprak kategorilere düzleştirir; üst kategoriler optgroup etiketi olur (seçilemez). */
function flatten(nodes: CategoryNode[], path: string[], groups: Map<string, Group>) {
  for (const node of nodes) {
    if (node.children.length > 0) {
      flatten(node.children, [...path, node.name], groups)
    } else {
      const label = path.join(' › ')
      const group = groups.get(label) ?? { label, leaves: [] }
      group.leaves.push(node)
      groups.set(label, group)
    }
  }
}

export function CategorySelect({ value, onChange, error }: { value: string; onChange: (value: string) => void; error?: string }) {
  const categories = useQuery({ queryKey: ['categories'], queryFn: ({ signal }) => sellerApi.categories(signal), staleTime: 10 * 60_000 })

  const groups = new Map<string, Group>()
  if (categories.data) flatten(categories.data, [], groups)
  const ungrouped = groups.get('')
  const grouped = [...groups.values()].filter((g) => g.label !== '')

  return (
    <SelectField
      label="Kategori"
      name="categoryId"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      error={error ?? (categories.isError ? 'Kategoriler yüklenemedi. Sayfayı yenileyin.' : undefined)}
      hint="Yalnızca en alt (yaprak) kategoriler seçilebilir."
      disabled={categories.isPending}
    >
      <option value="">{categories.isPending ? 'Yükleniyor…' : 'Kategori seçin'}</option>
      {ungrouped?.leaves.map((c) => (
        <option key={c.id} value={c.id}>{c.name}</option>
      ))}
      {grouped.map((g) => (
        <optgroup key={g.label} label={g.label}>
          {g.leaves.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </optgroup>
      ))}
    </SelectField>
  )
}
