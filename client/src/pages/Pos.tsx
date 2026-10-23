import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api, message, money, openReceipt } from '../api'
import { can, useAuth } from '../auth'
import { Field, Modal, toast } from '../ui'
import type { Catalog, Order, OrderSummary, Product } from '../types'

export function PosPage() {
  const { user, branchId } = useAuth()
  const [params] = useSearchParams()
  const [terminalId, setTerminalId] = useState(Number(localStorage.getItem('saffron.terminal') || 0))
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [order, setOrder] = useState<Order | null>(null)
  const [cat, setCat] = useState(0)
  const [q, setQ] = useState('')
  const [type, setType] = useState('DineIn')
  const [tableId, setTableId] = useState<number | ''>(params.get('table') ? Number(params.get('table')) : '')
  const [waiterId, setWaiterId] = useState<number | ''>('')
  const [customerId, setCustomerId] = useState<number | ''>('')
  const [address, setAddress] = useState('')
  const [phone, setPhone] = useState('')
  const [picked, setPicked] = useState<Product | null>(null)
  const [mods, setMods] = useState<number[]>([])
  const [adds, setAdds] = useState<number[]>([])
  const [qty, setQty] = useState(1)
  const [notes, setNotes] = useState('')
  const [payOpen, setPayOpen] = useState(false)
  const [held, setHeld] = useState<OrderSummary[]>([])
  const [heldOpen, setHeldOpen] = useState(false)
  const [discount, setDiscount] = useState('')
  const [coupon, setCoupon] = useState('')
  const [opening, setOpening] = useState('5000')

  async function loadCatalog(nextTerminal = terminalId) {
    const data = await api<Catalog>(`/api/catalog?branchId=${branchId}&terminalId=${nextTerminal || ''}`)
    setCatalog(data)
    if (!nextTerminal && data.terminals[0]) {
      localStorage.setItem('saffron.terminal', String(data.terminals[0].id))
      setTerminalId(data.terminals[0].id)
    }
  }

  useEffect(() => {
    setOrder(null)
    loadCatalog().catch(error => toast(error instanceof Error ? message(error) : 'Catalog failed', 'err'))
  }, [branchId])

  const products = useMemo(() => {
    if (!catalog) return []
    return catalog.products.filter(item => item.isActive && (cat === 0 || item.categoryId === cat) && (
      !q || item.name.toLowerCase().includes(q.toLowerCase()) || item.barcode === q || item.sku === q
    ))
  }, [catalog, cat, q])

  async function ensureOrder() {
    if (order && order.paymentStatus !== 'Paid' && order.status !== 'Void') return order
    const created = await api<Order>('/api/orders', {
      method: 'POST',
      body: JSON.stringify({
        branchId,
        terminalId: terminalId || null,
        orderType: type,
        tableId: tableId || null,
        waiterId: waiterId || null,
        customerId: customerId || null,
        deliveryAddress: address || null,
        deliveryPhone: phone || null,
        reuseTable: true,
      }),
    })
    setOrder(created)
    return created
  }

  function choose(product: Product) {
    const needs = (product.modifierGroupIds.length + product.addonIds.length) > 0
    if (!needs) {
      add(product, [], [], 1, '').catch(error => toast(error instanceof Error ? message(error) : 'Could not add item', 'err'))
      return
    }
    setPicked(product)
    setMods([])
    setAdds([])
    setQty(1)
    setNotes('')
  }

  async function add(product: Product, modifierOptionIds: number[], addonIds: number[], count: number, note: string) {
    const current = await ensureOrder()
    const category = catalog?.categories.find(item => item.id === product.categoryId)?.name
    const course = category === 'Starters' ? 'Starter' : category === 'Dessert' ? 'Dessert' : category === 'Drinks' ? 'Drink' : 'Main'
    const next = await api<Order>(`/api/orders/${current.id}/lines`, {
      method: 'POST',
      body: JSON.stringify({ productId: product.id, qty: count, course, notes: note, modifierOptionIds, addonIds, discountAmount: 0 }),
    })
    setOrder(next)
    setPicked(null)
    setQ('')
  }

  async function changeQty(lineId: number, nextQty: number) {
    if (!order) return
    const line = order.lines.find(item => item.id === lineId)
    if (!line) return
    if (nextQty <= 0) {
      setOrder(await api<Order>(`/api/orders/${order.id}/lines/${lineId}`, { method: 'DELETE' }))
      return
    }
    setOrder(await api<Order>(`/api/orders/${order.id}/lines/${lineId}`, {
      method: 'PUT',
      body: JSON.stringify({
        productId: line.productId,
        qty: nextQty,
        discountAmount: line.discountAmount,
        course: line.course,
        notes: line.notes,
        modifierOptionIds: [],
        addonIds: [],
      }),
    }))
  }

  async function saveMeta(patch: Record<string, unknown>) {
    if (!order) return
    setOrder(await api<Order>(`/api/orders/${order.id}`, { method: 'PUT', body: JSON.stringify(patch) }))
  }

  async function applyDiscount() {
    if (!order) return
    const result = await api<{ approvalRequired: boolean; message: string; order?: Order }>(`/api/orders/${order.id}/discount`, {
      method: 'POST', body: JSON.stringify({ amount: Number(discount || 0) }),
    })
    toast(result.message, result.approvalRequired ? 'err' : 'ok')
    if (result.order) setOrder(result.order)
  }

  async function applyCoupon() {
    if (!order) return
    setOrder(await api<Order>(`/api/orders/${order.id}/coupon`, { method: 'POST', body: JSON.stringify({ code: coupon }) }))
    toast('Coupon updated')
  }

  async function kitchen() {
    if (!order) return
    setOrder(await api<Order>(`/api/orders/${order.id}/kitchen`, { method: 'POST' }))
    toast('Sent to the kitchen')
  }

  async function hold() {
    if (!order) return
    await api(`/api/orders/${order.id}/hold`, { method: 'POST' })
    setOrder(null)
    toast('Order held')
  }

  async function showHeld() {
    setHeld(await api<OrderSummary[]>(`/api/orders?branchId=${branchId}&status=Held`))
    setHeldOpen(true)
  }

  async function resume(id: number) {
    setOrder(await api<Order>(`/api/orders/${id}/resume`, { method: 'POST' }))
    setHeldOpen(false)
  }

  async function openShift() {
    const shift = await api(`/api/shifts/open`, { method: 'POST', body: JSON.stringify({ branchId, terminalId, openingCash: Number(opening || 0) }) })
    toast('Shift opened')
    setCatalog(current => current ? { ...current, shift: shift as Catalog['shift'] } : current)
  }

  const groups = picked ? catalog?.modifierGroups.filter(group => picked.modifierGroupIds.includes(group.id)) || [] : []
  const addonChoices = picked ? catalog?.addons.filter(addon => picked.addonIds.includes(addon.id)) || [] : []

  function toggleMod(groupId: number, optionId: number, max: number) {
    setMods(current => {
      const groupOptionIds = groups.find(group => group.id === groupId)?.options.map(option => option.id) || []
      const without = current.filter(id => !groupOptionIds.includes(id))
      if (current.includes(optionId)) return current.filter(id => id !== optionId)
      if (max <= 1) return [...without, optionId]
      return [...current, optionId]
    })
  }

  return (
    <div className="pos">
      <section>
        <div className="page-head">
          <div>
            <h1>Sell</h1>
            <p>{catalog?.branch.name} · {catalog?.shift ? `Shift open by ${catalog.shift.userName}` : 'No shift on this terminal'}</p>
          </div>
          <div className="row">
            <select className="inline" value={terminalId || ''} onChange={e => { const id = Number(e.target.value); localStorage.setItem('saffron.terminal', String(id)); setTerminalId(id); loadCatalog(id).catch(() => undefined) }}>
              {catalog?.terminals.map(terminal => <option key={terminal.id} value={terminal.id}>{terminal.name}</option>)}
            </select>
            <button className="btn ghost" onClick={() => showHeld().catch(error => toast(message(error), 'err'))}>Held</button>
            <button className="btn ghost" onClick={() => setOrder(null)}>New sale</button>
          </div>
        </div>
        <input className="inline search" placeholder="Search name, SKU, or barcode and press Enter" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => {
          if (e.key !== 'Enter' || !catalog) return
          const hit = catalog.products.find(item => item.barcode === q.trim() || item.name.toLowerCase() === q.trim().toLowerCase())
          if (hit) choose(hit)
        }} />
        <div className="chips" style={{ marginTop: 12 }}>
          <button className={cat === 0 ? 'chip on' : 'chip'} onClick={() => setCat(0)}>All</button>
          {catalog?.categories.filter(item => item.isActive).map(item => (
            <button key={item.id} className={cat === item.id ? 'chip on' : 'chip'} onClick={() => setCat(item.id)}>{item.name}</button>
          ))}
        </div>
        <div className="products">
          {products.map(product => (
            <button key={product.id} className="product" style={{ background: product.color }} onClick={() => choose(product)}>
              <b>{product.name}</b>
              <span>{money(product.price)}</span>
            </button>
          ))}
        </div>
      </section>
      <aside className="ticket">
        <h2>{order?.orderNo || 'New ticket'}</h2>
        {!catalog?.shift && can(user, 'pos.shift') && (
          <div className="shift-note">
            <Field label="Opening cash"><input value={opening} onChange={e => setOpening(e.target.value)} /></Field>
            <button className="btn good" style={{ marginTop: 8 }} onClick={() => openShift().catch(error => toast(message(error), 'err'))}>Open shift</button>
          </div>
        )}
        <div className="row">
          <select className="inline" value={type} onChange={e => { setType(e.target.value); if (order) saveMeta({ orderType: e.target.value }).catch(error => toast(message(error), 'err')) }}>
            <option value="DineIn">Dine-in</option>
            <option value="Takeaway">Takeaway</option>
            <option value="Delivery">Delivery</option>
            <option value="Online">Online</option>
          </select>
          {type === 'DineIn' && (
            <select className="inline" value={tableId} onChange={e => { const id = e.target.value ? Number(e.target.value) : ''; setTableId(id); if (order) saveMeta(id ? { tableId: id } : { clearTable: true }).catch(error => toast(message(error), 'err')) }}>
              <option value="">Table</option>
              {catalog?.tables.map(table => <option key={table.id} value={table.id}>{table.name} · {table.status}</option>)}
            </select>
          )}
        </div>
        <div className="row" style={{ marginTop: 8 }}>
          <select className="inline" value={waiterId} onChange={e => { const id = e.target.value ? Number(e.target.value) : ''; setWaiterId(id); if (order && id) saveMeta({ waiterId: id }).catch(error => toast(message(error), 'err')) }}>
            <option value="">Waiter</option>
            {catalog?.staff.filter(person => person.role === 'Waiter' || person.role === 'Cashier' || person.role === 'Manager').map(person => <option key={person.id} value={person.id}>{person.fullName}</option>)}
          </select>
          <select className="inline" value={customerId} onChange={e => { const id = e.target.value ? Number(e.target.value) : ''; setCustomerId(id); if (order) saveMeta(id ? { customerId: id } : { clearCustomer: true }).catch(error => toast(message(error), 'err')) }}>
            <option value="">Customer</option>
            {catalog?.customers.map(customer => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
          </select>
        </div>
        {(type === 'Delivery' || type === 'Online') && (
          <div className="grid" style={{ marginTop: 8 }}>
            <input className="inline" placeholder="Delivery address" value={address} onChange={e => setAddress(e.target.value)} onBlur={() => order && saveMeta({ deliveryAddress: address })} />
            <input className="inline" placeholder="Phone" value={phone} onChange={e => setPhone(e.target.value)} onBlur={() => order && saveMeta({ deliveryPhone: phone })} />
          </div>
        )}
        <div className="ticket-lines">
          {order?.lines.filter(line => !line.isVoid).map(line => (
            <div className="tline" key={line.id}>
              <div>
                <b>{line.name}</b>
                <small>{line.extras.map(extra => extra.name).join(', ')} {line.notes}</small>
                <div className="stepper">
                  <button className="btn ghost tiny" onClick={() => changeQty(line.id, line.qty - 1).catch(error => toast(message(error), 'err'))}>−</button>
                  <span>{line.qty}</span>
                  <button className="btn ghost tiny" onClick={() => changeQty(line.id, line.qty + 1).catch(error => toast(message(error), 'err'))}>+</button>
                </div>
              </div>
              <strong>{money(line.lineTotal)}</strong>
            </div>
          ))}
          {!order?.lines.some(line => !line.isVoid) && <div className="empty">Tap a dish to start the ticket.</div>}
        </div>
        <div className="totals">
          <div><span>Subtotal</span><span>{money(order?.subtotal)}</span></div>
          <div><span>Discount</span><span>{money(order?.discountTotal)}</span></div>
          <div><span>Service</span><span>{money(order?.serviceCharge)}</span></div>
          <div><span>Tax</span><span>{money(order?.taxTotal)}</span></div>
          <div className="grand"><span>Total</span><span>{money(order?.grandTotal)}</span></div>
          <div><span>Due</span><span>{money(order?.balance)}</span></div>
        </div>
        <div className="row" style={{ marginTop: 8 }}>
          <input className="inline" placeholder="Discount ৳" value={discount} onChange={e => setDiscount(e.target.value)} />
          <button className="btn ghost" disabled={!order || !can(user, 'pos.discount')} onClick={() => applyDiscount().catch(error => toast(message(error), 'err'))}>Apply</button>
        </div>
        <div className="row" style={{ marginTop: 8 }}>
          <input className="inline" placeholder="Coupon" value={coupon} onChange={e => setCoupon(e.target.value)} />
          <button className="btn ghost" disabled={!order} onClick={() => applyCoupon().catch(error => toast(message(error), 'err'))}>Use</button>
        </div>
        <div className="row" style={{ marginTop: 10 }}>
          <button className="btn ghost" disabled={!order} onClick={() => hold().catch(error => toast(message(error), 'err'))}>Hold</button>
          <button className="btn" disabled={!order} onClick={() => kitchen().catch(error => toast(message(error), 'err'))}>Send KOT</button>
          <button className="btn primary" disabled={!order || !can(user, 'pos.pay')} onClick={() => setPayOpen(true)}>Pay</button>
          {order?.receipt && <button className="btn ghost" onClick={() => openReceipt(order.id, 80).catch(error => toast(message(error), 'err'))}>Print</button>}
        </div>
      </aside>

      {picked && (
        <Modal title={picked.name} onClose={() => setPicked(null)}>
          {groups.map(group => (
            <div key={group.id}>
              <strong>{group.name}{group.required ? ' *' : ''}</strong>
              {group.options.map(option => (
                <button key={option.id} className={mods.includes(option.id) ? 'choice on' : 'choice'} onClick={() => toggleMod(group.id, option.id, group.maxSelect)}>
                  <span>{option.name}</span><span>{option.price ? money(option.price) : 'Included'}</span>
                </button>
              ))}
            </div>
          ))}
          {addonChoices.map(addon => (
            <button key={addon.id} className={adds.includes(addon.id) ? 'choice on' : 'choice'} onClick={() => setAdds(current => current.includes(addon.id) ? current.filter(id => id !== addon.id) : [...current, addon.id])}>
              <span>{addon.name}</span><span>{money(addon.price)}</span>
            </button>
          ))}
          <div className="row" style={{ marginTop: 8 }}>
            <Field label="Qty"><input value={qty} onChange={e => setQty(Number(e.target.value))} /></Field>
            <Field label="Note"><input value={notes} onChange={e => setNotes(e.target.value)} /></Field>
          </div>
          <button className="btn primary" style={{ marginTop: 12 }} onClick={() => add(picked, mods, adds, qty || 1, notes).catch(error => toast(message(error), 'err'))}>Add {money(picked.price * (qty || 1))}</button>
        </Modal>
      )}

      {payOpen && order && <PayModal order={order} onClose={() => setPayOpen(false)} onPaid={setOrder} />}
      {heldOpen && (
        <Modal title="Held orders" onClose={() => setHeldOpen(false)}>
          {held.map(item => (
            <button key={item.id} className="choice" onClick={() => resume(item.id).catch(error => toast(message(error), 'err'))}>
              <span>{item.orderNo} · {item.orderType}</span><span>{money(item.grandTotal)}</span>
            </button>
          ))}
          {held.length === 0 && <div className="empty">No held orders.</div>}
        </Modal>
      )}
    </div>
  )
}

function PayModal({ order, onClose, onPaid }: { order: Order; onClose: () => void; onPaid: (order: Order) => void }) {
  const [method, setMethod] = useState('Cash')
  const [amount, setAmount] = useState(String(order.balance))
  const [tendered, setTendered] = useState(String(order.balance))
  const [reference, setReference] = useState('')
  const due = Number(amount || 0)
  const cash = Number(tendered || 0)
  const change = method === 'Cash' ? Math.max(0, cash - due) : 0

  async function pay() {
    const result = await api<{ order: Order; changeDue: number; message: string }>(`/api/orders/${order.id}/pay`, {
      method: 'POST',
      body: JSON.stringify({ method, amount: due, tendered: method === 'Cash' ? cash : due, reference: reference || null, customerId: order.customerId }),
    })
    toast(result.message)
    onPaid(result.order)
    if (result.order.balance <= 0.009) onClose()
    else { setAmount(String(result.order.balance)); setTendered(String(result.order.balance)) }
  }

  return (
    <Modal title={'Pay ' + order.orderNo} onClose={onClose}>
      <p>Balance {money(order.balance)}</p>
      <div className="row">
        {['Cash', 'Card', 'Mobile', 'Due'].map(item => (
          <button key={item} className={method === item ? 'chip on' : 'chip'} onClick={() => setMethod(item)}>{item}</button>
        ))}
      </div>
      <div className="grid" style={{ marginTop: 10 }}>
        <Field label="Amount"><input value={amount} onChange={e => setAmount(e.target.value)} /></Field>
        {method === 'Cash' && <Field label="Cash received"><input value={tendered} onChange={e => setTendered(e.target.value)} /></Field>}
        {method !== 'Cash' && <Field label="Reference"><input value={reference} onChange={e => setReference(e.target.value)} /></Field>}
      </div>
      <div className="row" style={{ marginTop: 8 }}>
        <button className="btn ghost" onClick={() => { setAmount(String(order.balance)); setTendered(String(order.balance)) }}>Exact</button>
        <button className="btn ghost" onClick={() => setAmount((order.balance / 2).toFixed(2))}>Half</button>
      </div>
      {method === 'Cash' && <p>Change {money(change)}</p>}
      {method === 'Due' && !order.customerId && <p className="badge warn">Select a customer on the ticket before a due payment.</p>}
      <button className="btn primary" onClick={() => pay().catch(error => toast(error instanceof Error ? message(error) : 'Payment failed', 'err'))}>Take payment</button>
    </Modal>
  )
}
