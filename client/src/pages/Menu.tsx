import { useEffect, useState } from 'react'
import { api, message, money } from '../api'
import { useAuth } from '../auth'
import { Field, toast } from '../ui'
import type { Catalog, Product } from '../types'

const blank = { name: '', categoryId: 0, price: '', costPrice: '', barcode: '', prepMinutes: '15', modifierGroupIds: [] as number[], addonIds: [] as number[] }

export function MenuPage() {
  const { branchId } = useAuth()
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [form, setForm] = useState(blank)
  const [editing, setEditing] = useState<number | null>(null)
  function load() { api<Catalog>(`/api/catalog?branchId=${branchId}`).then(setCatalog).catch(error => toast(message(error), 'err')) }
  useEffect(() => { load() }, [branchId])

  function edit(product: Product) {
    setEditing(product.id)
    setForm({
      name: product.name, categoryId: product.categoryId, price: String(product.price), costPrice: String(product.costPrice),
      barcode: product.barcode || '', prepMinutes: String(product.prepMinutes), modifierGroupIds: product.modifierGroupIds, addonIds: product.addonIds,
    })
  }

  async function save() {
    await api('/api/products', {
      method: 'POST',
      body: JSON.stringify({
        id: editing, categoryId: form.categoryId || catalog?.categories[0]?.id, name: form.name,
        price: Number(form.price), costPrice: Number(form.costPrice || 0), barcode: form.barcode || null,
        prepMinutes: Number(form.prepMinutes || 15), modifierGroupIds: form.modifierGroupIds, addonIds: form.addonIds, isActive: true,
      }),
    })
    setForm(blank); setEditing(null); toast('Menu item saved'); load()
  }

  return (
    <div className="grid" style={{ gridTemplateColumns: '1.2fr .8fr' }}>
      <div>
        <div className="page-head"><div><h1>Menu</h1><p>Prices, barcodes, modifiers, and add-ons.</p></div></div>
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Item</th><th>Price</th><th>Cost</th><th>Barcode</th></tr></thead>
            <tbody>
              {catalog?.products.map(product => (
                <tr key={product.id} onClick={() => edit(product)} style={{ cursor: 'pointer' }}>
                  <td>{product.name}<div className="muted">{catalog.categories.find(c => c.id === product.categoryId)?.name}</div></td>
                  <td>{money(product.price)}</td>
                  <td>{money(product.costPrice)}</td>
                  <td>{product.barcode}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      <div className="card grid">
        <h3>{editing ? 'Edit item' : 'New item'}</h3>
        <Field label="Name"><input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Category">
          <select value={form.categoryId || catalog?.categories[0]?.id || ''} onChange={e => setForm({ ...form, categoryId: Number(e.target.value) })}>
            {catalog?.categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
          </select>
        </Field>
        <Field label="Price"><input value={form.price} onChange={e => setForm({ ...form, price: e.target.value })} /></Field>
        <Field label="Cost"><input value={form.costPrice} onChange={e => setForm({ ...form, costPrice: e.target.value })} /></Field>
        <Field label="Barcode"><input value={form.barcode} onChange={e => setForm({ ...form, barcode: e.target.value })} /></Field>
        <div>
          <span className="muted">Modifiers</span>
          {catalog?.modifierGroups.map(group => (
            <label key={group.id} className="choice">
              <span>{group.name}</span>
              <input type="checkbox" checked={form.modifierGroupIds.includes(group.id)} onChange={() => setForm(current => ({ ...current, modifierGroupIds: current.modifierGroupIds.includes(group.id) ? current.modifierGroupIds.filter(id => id !== group.id) : [...current.modifierGroupIds, group.id] }))} />
            </label>
          ))}
        </div>
        <div>
          <span className="muted">Add-ons</span>
          {catalog?.addons.map(addon => (
            <label key={addon.id} className="choice">
              <span>{addon.name} · {money(addon.price)}</span>
              <input type="checkbox" checked={form.addonIds.includes(addon.id)} onChange={() => setForm(current => ({ ...current, addonIds: current.addonIds.includes(addon.id) ? current.addonIds.filter(id => id !== addon.id) : [...current.addonIds, addon.id] }))} />
            </label>
          ))}
        </div>
        <button className="btn primary" onClick={() => save().catch(error => toast(message(error), 'err'))}>Save item</button>
      </div>
    </div>
  )
}
