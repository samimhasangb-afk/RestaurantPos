import { useEffect, useState } from 'react'
import { api, message, money, openReceipt, when } from '../api'
import { useAuth } from '../auth'
import { Empty, Field, toast } from '../ui'
import type { Order, OrderSummary } from '../types'

export function ReceiptsPage() {
  const { branchId } = useAuth()
  const [rows, setRows] = useState<OrderSummary[]>([])
  const [order, setOrder] = useState<Order | null>(null)
  const [channel, setChannel] = useState('Email')
  const [destination, setDestination] = useState('')
  useEffect(() => { api<OrderSummary[]>(`/api/orders?branchId=${branchId}&status=Paid`).then(setRows).catch(error => toast(message(error), 'err')) }, [branchId])
  return (
    <div>
      <div className="page-head"><div><h1>Receipts</h1><p>Reprint 58mm or 80mm, or queue an email or SMS copy.</p></div></div>
      <div className="card table-wrap">
        <table>
          <thead><tr><th>Order</th><th>Total</th><th>When</th><th></th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id}>
                <td>{row.orderNo}<div className="muted">{row.customerName}</div></td>
                <td>{money(row.grandTotal)}</td>
                <td>{when(row.createdAt)}</td>
                <td className="row">
                  <button className="btn ghost tiny" onClick={() => openReceipt(row.id, 80).catch(error => toast(message(error), 'err'))}>80mm</button>
                  <button className="btn ghost tiny" onClick={() => openReceipt(row.id, 58).catch(error => toast(message(error), 'err'))}>58mm</button>
                  <button className="btn ghost tiny" onClick={() => api<Order>('/api/orders/' + row.id).then(setOrder)}>Send</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {rows.length === 0 && <Empty text="No paid receipts yet." />}
      </div>
      {order && (
        <div className="card" style={{ marginTop: 12 }}>
          <h3>Digital receipt for {order.orderNo}</h3>
          <div className="row">
            <select value={channel} onChange={e => setChannel(e.target.value)}><option>Email</option><option>Sms</option></select>
            <Field label="Send to"><input value={destination} onChange={e => setDestination(e.target.value)} /></Field>
            <button className="btn" onClick={() => api(`/api/orders/${order.id}/dispatch`, { method: 'POST', body: JSON.stringify({ channel, destination }) }).then(() => toast('Receipt saved in the audit log')).catch(error => toast(message(error), 'err'))}>Queue</button>
          </div>
          {order.receipt?.dispatches.map((item, index) => <p key={index}>{item.channel} · {item.destination} · {item.status}</p>)}
        </div>
      )}
    </div>
  )
}
