import { useEffect, useState } from 'react'
import { api, message, money, qty, when } from '../api'
import { useAuth } from '../auth'
import { Field, toast } from '../ui'
import type { Branch, Ingredient, LedgerRow, StockRow } from '../types'

export function InventoryPage() {
  const { branchId, user } = useAuth()
  const [tab, setTab] = useState('stock')
  const [stock, setStock] = useState<StockRow[]>([])
  const [ingredients, setIngredients] = useState<Ingredient[]>([])
  const [ledger, setLedger] = useState<LedgerRow[]>([])
  const [branches, setBranches] = useState<Branch[]>([])
  const [name, setName] = useState('')
  const [unit, setUnit] = useState('kg')
  const [reorder, setReorder] = useState('1')
  const [cost, setCost] = useState('')
  const [move, setMove] = useState({ ingredientId: 0, quantity: '', reason: '', toBranchId: 0, counted: '' })

  function load() {
    api<StockRow[]>(`/api/stock?branchId=${branchId}`).then(setStock).catch(error => toast(message(error), 'err'))
    api<Ingredient[]>('/api/ingredients').then(setIngredients).catch(() => undefined)
    api<LedgerRow[]>(`/api/stock/ledger?branchId=${branchId}`).then(setLedger).catch(() => undefined)
    api<Branch[]>('/api/admin/branches').then(setBranches).catch(() => setBranches(user?.branches || []))
  }
  useEffect(() => { load() }, [branchId])

  async function saveIngredient() {
    await api('/api/ingredients', { method: 'POST', body: JSON.stringify({ name, unit, reorderLevel: Number(reorder), costPerUnit: Number(cost), isActive: true }) })
    setName(''); toast('Ingredient saved'); load()
  }

  const body = { branchId, ingredientId: Number(move.ingredientId), quantity: Number(move.quantity), qtyDelta: Number(move.quantity), reason: move.reason }

  return (
    <div>
      <div className="page-head"><div><h1>Inventory</h1><p>On-hand stock, recipes, wastage, counts, and transfers.</p></div></div>
      <div className="chips">
        {['stock', 'ledger', 'move', 'ingredient'].map(item => <button key={item} className={tab === item ? 'chip on' : 'chip'} onClick={() => setTab(item)}>{item}</button>)}
      </div>
      {tab === 'stock' && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Ingredient</th><th>On hand</th><th>Value</th><th>Reorder</th><th>Expiry</th></tr></thead>
            <tbody>
              {stock.map(row => (
                <tr key={row.ingredientId}>
                  <td>{row.name} {row.low && <span className="badge bad">Low</span>} {row.expiring && <span className="badge warn">Expiry</span>}</td>
                  <td>{qty(row.quantity)} {row.unit}</td>
                  <td>{money(row.value)}</td>
                  <td>{qty(row.reorderLevel)}</td>
                  <td>{row.expiryDate ? when(row.expiryDate) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {tab === 'ledger' && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>When</th><th>Item</th><th>Type</th><th>Qty</th><th>Balance</th><th>Note</th></tr></thead>
            <tbody>
              {ledger.map(row => <tr key={row.id}><td>{when(row.createdAt)}</td><td>{row.ingredientName}</td><td>{row.txnType}</td><td>{qty(row.quantity)}</td><td>{qty(row.balanceAfter)}</td><td>{row.note}</td></tr>)}
            </tbody>
          </table>
        </div>
      )}
      {tab === 'move' && (
        <div className="card grid" style={{ maxWidth: 560 }}>
          <Field label="Ingredient">
            <select value={move.ingredientId} onChange={e => setMove({ ...move, ingredientId: Number(e.target.value) })}>
              <option value={0}>Choose</option>
              {ingredients.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </Field>
          <Field label="Quantity"><input value={move.quantity} onChange={e => setMove({ ...move, quantity: e.target.value })} /></Field>
          <Field label="Reason"><input value={move.reason} onChange={e => setMove({ ...move, reason: e.target.value })} /></Field>
          <div className="row">
            <button className="btn danger" onClick={() => api('/api/stock/wastage', { method: 'POST', body: JSON.stringify(body) }).then(() => { toast('Wastage recorded'); load() }).catch(error => toast(message(error), 'err'))}>Wastage</button>
            <button className="btn" onClick={() => api<{ message: string; approvalRequired?: boolean }>('/api/stock/adjustments', { method: 'POST', body: JSON.stringify(body) }).then(res => { toast(res.message, res.approvalRequired ? 'err' : 'ok'); load() }).catch(error => toast(message(error), 'err'))}>Adjust</button>
          </div>
          <Field label="Counted quantity"><input value={move.counted} onChange={e => setMove({ ...move, counted: e.target.value })} /></Field>
          <button className="btn ghost" onClick={() => api('/api/stock/counts', { method: 'POST', body: JSON.stringify({ branchId, lines: [{ ingredientId: Number(move.ingredientId), countedQty: Number(move.counted) }] }) }).then(() => { toast('Count posted'); load() }).catch(error => toast(message(error), 'err'))}>Post physical count</button>
          <Field label="Transfer to">
            <select value={move.toBranchId} onChange={e => setMove({ ...move, toBranchId: Number(e.target.value) })}>
              <option value={0}>Branch</option>
              {branches.filter(item => item.id !== branchId).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </Field>
          <button className="btn ghost" onClick={() => api('/api/stock/transfers', { method: 'POST', body: JSON.stringify({ fromBranchId: branchId, toBranchId: move.toBranchId, note: move.reason, lines: [{ ingredientId: Number(move.ingredientId), quantity: Number(move.quantity) }] }) }).then(() => { toast('Transferred'); load() }).catch(error => toast(message(error), 'err'))}>Transfer</button>
        </div>
      )}
      {tab === 'ingredient' && (
        <div className="card grid" style={{ maxWidth: 480 }}>
          <Field label="Name"><input value={name} onChange={e => setName(e.target.value)} /></Field>
          <Field label="Unit"><input value={unit} onChange={e => setUnit(e.target.value)} /></Field>
          <Field label="Reorder level"><input value={reorder} onChange={e => setReorder(e.target.value)} /></Field>
          <Field label="Cost per unit"><input value={cost} onChange={e => setCost(e.target.value)} /></Field>
          <button className="btn primary" onClick={() => saveIngredient().catch(error => toast(message(error), 'err'))}>Save ingredient</button>
        </div>
      )}
    </div>
  )
}
