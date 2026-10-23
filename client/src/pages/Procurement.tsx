import { useEffect, useState } from 'react'
import { api, message, money, when } from '../api'
import { useAuth } from '../auth'
import { Field, toast } from '../ui'
import type { Ingredient, PurchaseOrder, Supplier } from '../types'

export function ProcurementPage() {
  const { branchId } = useAuth()
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [orders, setOrders] = useState<PurchaseOrder[]>([])
  const [ingredients, setIngredients] = useState<Ingredient[]>([])
  const [supplier, setSupplier] = useState({ name: '', phone: '' })
  const [po, setPo] = useState({ supplierId: 0, ingredientId: 0, quantity: '', unitCost: '' })
  const [receive, setReceive] = useState<Record<number, string>>({})

  function load() {
    api<Supplier[]>('/api/suppliers').then(setSuppliers).catch(error => toast(message(error), 'err'))
    api<PurchaseOrder[]>(`/api/purchase-orders?branchId=${branchId}`).then(setOrders).catch(error => toast(message(error), 'err'))
    api<Ingredient[]>('/api/ingredients').then(setIngredients).catch(() => undefined)
  }
  useEffect(() => { load() }, [branchId])

  return (
    <div className="grid" style={{ gridTemplateColumns: '1fr 1fr' }}>
      <div>
        <div className="page-head"><div><h1>Purchasing</h1><p>Suppliers, purchase orders, and goods receiving.</p></div></div>
        {orders.map(order => (
          <article key={order.id} className="card" style={{ marginBottom: 10 }}>
            <strong>{order.poNo}</strong> · {order.supplierName} · <span className="badge">{order.status}</span>
            <div className="muted">{when(order.orderDate)} · {money(order.total)}</div>
            {order.lines.map(line => (
              <div key={line.id} className="tline">
                <span>{line.ingredientName} · received {line.receivedQty} / {line.quantity}</span>
                {order.status !== 'Received' && <input className="inline" style={{ width: 80 }} placeholder="Qty" value={receive[line.id] || ''} onChange={e => setReceive({ ...receive, [line.id]: e.target.value })} />}
              </div>
            ))}
            {order.status !== 'Received' && <button className="btn" onClick={() => api(`/api/purchase-orders/${order.id}/receive`, { method: 'POST', body: JSON.stringify({ lines: order.lines.filter(line => Number(receive[line.id]) > 0).map(line => ({ poLineId: line.id, quantity: Number(receive[line.id]) })) }) }).then(() => { toast('Goods received'); load() }).catch(error => toast(message(error), 'err'))}>Receive</button>}
          </article>
        ))}
      </div>
      <div className="grid">
        <div className="card grid">
          <h3>New purchase</h3>
          <Field label="Supplier">
            <select value={po.supplierId} onChange={e => setPo({ ...po, supplierId: Number(e.target.value) })}>
              <option value={0}>Choose</option>
              {suppliers.map(item => <option key={item.id} value={item.id}>{item.name} · due {money(item.dueBalance)}</option>)}
            </select>
          </Field>
          <Field label="Ingredient">
            <select value={po.ingredientId} onChange={e => setPo({ ...po, ingredientId: Number(e.target.value) })}>
              <option value={0}>Choose</option>
              {ingredients.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </Field>
          <Field label="Quantity"><input value={po.quantity} onChange={e => setPo({ ...po, quantity: e.target.value })} /></Field>
          <Field label="Unit cost"><input value={po.unitCost} onChange={e => setPo({ ...po, unitCost: e.target.value })} /></Field>
          <button className="btn primary" onClick={() => api('/api/purchase-orders', { method: 'POST', body: JSON.stringify({ branchId, supplierId: po.supplierId, lines: [{ ingredientId: po.ingredientId, quantity: Number(po.quantity), unitCost: Number(po.unitCost) }] }) }).then(() => { toast('Purchase order created'); load() }).catch(error => toast(message(error), 'err'))}>Create PO</button>
        </div>
        <div className="card grid">
          <h3>Supplier</h3>
          {suppliers.map(item => (
            <div key={item.id} className="tline">
              <span>{item.name}<small style={{ display: 'block' }}>{item.phone}</small></span>
              <span>{money(item.dueBalance)} <button className="btn ghost tiny" onClick={() => { const amount = Number(prompt('Payment amount') || 0); if (amount > 0) api(`/api/suppliers/${item.id}/pay`, { method: 'POST', body: JSON.stringify({ amount, method: 'Cash' }) }).then(() => { toast('Payment recorded'); load() }).catch(error => toast(message(error), 'err')) }}>Pay</button></span>
            </div>
          ))}
          <Field label="Name"><input value={supplier.name} onChange={e => setSupplier({ ...supplier, name: e.target.value })} /></Field>
          <Field label="Phone"><input value={supplier.phone} onChange={e => setSupplier({ ...supplier, phone: e.target.value })} /></Field>
          <button className="btn" onClick={() => api('/api/suppliers', { method: 'POST', body: JSON.stringify({ ...supplier, isActive: true }) }).then(() => { toast('Supplier saved'); load() }).catch(error => toast(message(error), 'err'))}>Save supplier</button>
        </div>
      </div>
    </div>
  )
}
