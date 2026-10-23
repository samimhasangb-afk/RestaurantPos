import { useEffect, useState } from 'react'
import { api, message, money, todayInput } from '../api'
import { useAuth } from '../auth'
import { toast } from '../ui'
import type { Overview } from '../types'

export function ReportsPage() {
  const { branchId } = useAuth()
  const [from, setFrom] = useState(todayInput())
  const [to, setTo] = useState(todayInput())
  const [all, setAll] = useState(false)
  const [data, setData] = useState<Overview | null>(null)

  function load(nextAll = all) {
    api<Overview>(`/api/reports/overview?branchId=${nextAll ? 0 : branchId}&from=${from}&to=${to}`).then(setData).catch(error => toast(message(error), 'err'))
  }
  useEffect(() => { load() }, [branchId])

  function download() {
    if (!data) return
    const lines = [['Section', 'Name', 'Amount', 'Qty']]
    const push = (section: string, rows: { name: string; amount: number; qty: number }[]) => rows.forEach(row => lines.push([section, row.name, String(row.amount), String(row.qty)]))
    lines.push(['Summary', 'Net sales', String(data.netSales), ''])
    lines.push(['Summary', 'Gross profit', String(data.grossProfit), ''])
    push('Payment', data.byPayment)
    push('Cashier', data.byCashier)
    push('Best', data.bestSellers)
    push('Slow', data.slowMovers)
    push('Branch', data.byBranch)
    const csv = lines.map(line => line.join(',')).join('\n')
    const blob = new Blob([csv], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `saffron-${from}-${to}.csv`
    link.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div>
      <div className="page-head">
        <div><h1>Reports</h1><p>Sales, food cost, cashiers, products, and branches.</p></div>
        <button className="btn ghost" onClick={download}>Download CSV</button>
      </div>
      <div className="filters">
        <input type="date" value={from} onChange={e => setFrom(e.target.value)} />
        <input type="date" value={to} onChange={e => setTo(e.target.value)} />
        <label><input type="checkbox" checked={all} onChange={e => { setAll(e.target.checked); load(e.target.checked) }} /> All my branches</label>
        <button className="btn" onClick={() => load()}>Run</button>
      </div>
      {data && (
        <>
          <div className="stats">
            <div className="stat"><span>Net sales</span><strong>{money(data.netSales)}</strong></div>
            <div className="stat"><span>Gross profit</span><strong>{money(data.grossProfit)}</strong></div>
            <div className="stat"><span>Food cost</span><strong>{money(data.foodCost)}</strong></div>
            <div className="stat"><span>Wastage</span><strong>{money(data.wastageCost)}</strong></div>
          </div>
          <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', marginTop: 12 }}>
            <List title="Best selling" rows={data.bestSellers} />
            <List title="Slow moving" rows={data.slowMovers} />
            <List title="Cashier sales" rows={data.byCashier} />
            <List title="Payment reconciliation" rows={data.byPayment} />
            <List title="Branch comparison" rows={data.byBranch} />
            <div className="card"><h3>Accounts</h3><p>Expenses {money(data.expenses)}</p><p>Customer dues {money(data.customerDues)}</p><p>Supplier dues {money(data.supplierDues)}</p><p>Tax {money(data.tax)} · Service {money(data.serviceCharge)} · Refunds {money(data.refunds)}</p></div>
          </div>
        </>
      )}
    </div>
  )
}

function List({ title, rows }: { title: string; rows: { name: string; amount: number; qty: number; count: number }[] }) {
  return (
    <div className="card">
      <h3>{title}</h3>
      {rows.map(row => <div className="tline" key={row.name}><span>{row.name}{row.qty ? ` · ${row.qty}` : ''}</span><b>{money(row.amount)}</b></div>)}
    </div>
  )
}
