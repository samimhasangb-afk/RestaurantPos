import { useEffect, useState } from 'react'
import { api, money, todayInput } from '../api'
import { can, useAuth } from '../auth'
import { Empty } from '../ui'
import type { Overview } from '../types'

export function DashboardPage() {
  const { user, branchId } = useAuth()
  const [data, setData] = useState<Overview | null>(null)
  const [error, setError] = useState('')
  useEffect(() => {
    if (!can(user, 'reports.view')) { setError('Your role opens on another screen.'); return }
    const day = todayInput()
    api<Overview>(`/api/reports/overview?branchId=${branchId}&from=${day}&to=${day}`).then(setData).catch(err => setError(err instanceof Error ? err.message : 'Failed'))
  }, [branchId, user])
  if (error) return <Empty text={error} />
  if (!data) return <Empty text="Loading today's service…" />
  const max = Math.max(...data.daily.map(item => item.amount), 1)
  return (
    <div>
      <div className="page-head"><div><h1>Today</h1><p>{data.from} · {data.orderCount} settled tickets in view</p></div></div>
      <div className="stats">
        <div className="stat"><span>Net sales</span><strong>{money(data.netSales)}</strong><em>Gross {money(data.grossSales)}</em></div>
        <div className="stat"><span>Gross profit</span><strong>{money(data.grossProfit)}</strong><em>Food cost {money(data.foodCost)}</em></div>
        <div className="stat"><span>Open orders</span><strong>{data.openOrders}</strong><em>{data.pendingApprovals} approvals waiting</em></div>
        <div className="stat"><span>Low stock</span><strong>{data.lowStockCount}</strong><em>Dues {money(data.customerDues)}</em></div>
      </div>
      <div className="grid" style={{ gridTemplateColumns: '1.4fr 1fr', marginTop: 14 }}>
        <div className="card">
          <h3>Recent days</h3>
          <div className="bars">
            {data.daily.map(item => <div key={item.name} style={{ height: `${Math.max(8, item.amount / max * 100)}%` }} title={money(item.amount)}><span>{item.name}</span></div>)}
          </div>
        </div>
        <div className="card">
          <h3>Payments</h3>
          {data.byPayment.map(item => <div className="tline" key={item.name}><span>{item.name}</span><b>{money(item.amount)}</b></div>)}
          {data.byPayment.length === 0 && <Empty text="No payments yet today." />}
        </div>
      </div>
    </div>
  )
}
