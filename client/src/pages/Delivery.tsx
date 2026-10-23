import { useEffect, useState } from 'react'
import { api, message, money, when } from '../api'
import { useAuth } from '../auth'
import { Empty, toast } from '../ui'
import type { Catalog, OrderSummary } from '../types'

const steps = ['Pending', 'Assigned', 'PickedUp', 'OnTheWay', 'Delivered']

export function DeliveryPage() {
  const { branchId } = useAuth()
  const [rows, setRows] = useState<OrderSummary[]>([])
  const [staff, setStaff] = useState<Catalog['staff']>([])
  function load() {
    api<OrderSummary[]>(`/api/orders?branchId=${branchId}`).then(list => setRows(list.filter(row => row.orderType === 'Delivery' || row.orderType === 'Online'))).catch(error => toast(message(error), 'err'))
  }
  useEffect(() => {
    load()
    api<Catalog>(`/api/catalog?branchId=${branchId}`).then(data => setStaff(data.staff)).catch(() => undefined)
  }, [branchId])

  async function move(id: number, status: string) {
    await api(`/api/orders/${id}`, { method: 'PUT', body: JSON.stringify({ deliveryStatus: status }) })
    load()
  }

  return (
    <div>
      <div className="page-head"><div><h1>Delivery</h1><p>Online and delivery tickets, from the pass to the door.</p></div></div>
      {rows.length === 0 && <Empty text="No delivery orders." />}
      <div className="kds">
        {rows.map(row => (
          <article key={row.id} className="kcard">
            <strong>{row.orderNo}</strong>
            <p>{row.customerName || 'Guest'} · {money(row.grandTotal)} · {when(row.createdAt)}</p>
            <div className="chips">
              {steps.map(step => <button key={step} className={row.deliveryStatus === step ? 'chip on' : 'chip'} onClick={() => move(row.id, step).catch(error => toast(message(error), 'err'))}>{step}</button>)}
            </div>
            <select className="inline" style={{ marginTop: 8 }} onChange={e => api(`/api/orders/${row.id}`, { method: 'PUT', body: JSON.stringify({ riderId: Number(e.target.value), deliveryStatus: 'Assigned' }) }).then(load)}>
              <option value="">Assign rider</option>
              {staff.map(person => <option key={person.id} value={person.id}>{person.fullName}</option>)}
            </select>
          </article>
        ))}
      </div>
    </div>
  )
}
