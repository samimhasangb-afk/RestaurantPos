import { useEffect, useState } from 'react'
import { api, message, money, openReceipt, when } from '../api'
import { can, useAuth } from '../auth'
import { Empty, Field, Modal, toast } from '../ui'
import type { ActionResult, Order, OrderSummary } from '../types'

export function OrdersPage() {
  const { user, branchId } = useAuth()
  const [rows, setRows] = useState<OrderSummary[]>([])
  const [status, setStatus] = useState('All')
  const [q, setQ] = useState('')
  const [current, setCurrent] = useState<Order | null>(null)
  const [reason, setReason] = useState('')
  const [amount, setAmount] = useState('')

  function load() {
    api<OrderSummary[]>(`/api/orders?branchId=${branchId}&status=${status}&q=${encodeURIComponent(q)}`).then(setRows).catch(error => toast(message(error), 'err'))
  }
  useEffect(() => { load() }, [branchId, status])

  async function open(id: number) {
    const order = await api<Order>('/api/orders/' + id)
    setCurrent(order)
    setAmount(String(Math.max(0, order.paidTotal - order.refundedTotal)))
    setReason('')
  }

  async function act(kind: 'refund' | 'void') {
    if (!current) return
    const path = kind === 'refund' ? `/api/orders/${current.id}/refund` : `/api/orders/${current.id}/void`
    const body = kind === 'refund' ? { amount: Number(amount), reason, method: 'Cash', restock: true } : { reason }
    const result = await api<ActionResult>(path, { method: 'POST', body: JSON.stringify(body) })
    toast(result.message, result.approvalRequired ? 'err' : 'ok')
    if (result.order) setCurrent(result.order)
    load()
  }

  return (
    <div>
      <div className="page-head"><div><h1>Orders</h1><p>Hold, resume, refund, void, and reprint from one list.</p></div></div>
      <div className="filters">
        <select value={status} onChange={e => setStatus(e.target.value)}>
          {['All', 'Open', 'Held', 'Unpaid', 'Partial', 'Paid', 'Refunded', 'Void', 'Submitted', 'Ready'].map(item => <option key={item}>{item}</option>)}
        </select>
        <input placeholder="Order no or customer" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => { if (e.key === 'Enter') load() }} />
        <button className="btn ghost" onClick={load}>Search</button>
      </div>
      <div className="card table-wrap">
        <table>
          <thead><tr><th>Order</th><th>Type</th><th>Kitchen</th><th>Payment</th><th>Total</th><th>Due</th><th>When</th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id} onClick={() => open(row.id).catch(error => toast(message(error), 'err'))} style={{ cursor: 'pointer' }}>
                <td>{row.orderNo}<div className="muted">{row.tableName || row.customerName || row.orderType}</div></td>
                <td>{row.orderType}</td>
                <td><span className="badge">{row.kitchenStatus}</span></td>
                <td><span className={row.paymentStatus === 'Paid' ? 'badge good' : 'badge'}>{row.paymentStatus}</span></td>
                <td>{money(row.grandTotal)}</td>
                <td>{money(row.balance)}</td>
                <td>{when(row.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {rows.length === 0 && <Empty text="No orders match." />}
      </div>
      {current && (
        <Modal wide title={current.orderNo} onClose={() => setCurrent(null)}>
          <p>{current.orderType} · {current.paymentStatus} · {current.kitchenStatus}</p>
          {current.lines.filter(line => !line.isVoid).map(line => <div className="tline" key={line.id}><span>{line.qty} × {line.name}</span><b>{money(line.lineTotal)}</b></div>)}
          <div className="totals"><div className="grand"><span>Total</span><span>{money(current.grandTotal)}</span></div></div>
          <div className="row">
            <button className="btn ghost" onClick={() => openReceipt(current.id).catch(error => toast(message(error), 'err'))}>Reprint</button>
            {current.status === 'Held' && <button className="btn" onClick={() => api(`/api/orders/${current.id}/resume`, { method: 'POST' }).then(() => open(current.id)).catch(error => toast(message(error), 'err'))}>Resume</button>}
          </div>
          {(can(user, 'pos.refund') || can(user, 'pos.void') || can(user, 'pos.order')) && (
            <div className="grid" style={{ marginTop: 12 }}>
              <Field label="Reason"><input value={reason} onChange={e => setReason(e.target.value)} /></Field>
              <Field label="Refund amount"><input value={amount} onChange={e => setAmount(e.target.value)} /></Field>
              <div className="row">
                <button className="btn danger" onClick={() => act('refund').catch(error => toast(message(error), 'err'))}>Refund</button>
                <button className="btn ghost" onClick={() => act('void').catch(error => toast(message(error), 'err'))}>Void</button>
              </div>
            </div>
          )}
          {current.receipt && <p className="muted">Receipt {current.receipt.receiptNo} · printed {current.receipt.printCount} times</p>}
        </Modal>
      )}
    </div>
  )
}
