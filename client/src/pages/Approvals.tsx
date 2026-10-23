import { useEffect, useState } from 'react'
import { api, message, money, when } from '../api'
import { toast } from '../ui'
import type { Approval } from '../types'

export function ApprovalsPage() {
  const [rows, setRows] = useState<Approval[]>([])
  function load() { api<Approval[]>('/api/admin/approvals').then(setRows).catch(error => toast(message(error), 'err')) }
  useEffect(() => { load() }, [])
  async function review(id: number, approve: boolean) {
    await api(`/api/admin/approvals/${id}/${approve ? 'approve' : 'reject'}`, { method: 'POST', body: JSON.stringify({ note: approve ? 'Approved' : 'Rejected' }) })
    toast(approve ? 'Approved' : 'Rejected')
    load()
  }
  return (
    <div>
      <div className="page-head"><div><h1>Approvals</h1><p>Discounts, refunds, voids, and sensitive stock adjustments.</p></div></div>
      <div className="card table-wrap">
        <table>
          <thead><tr><th>When</th><th>Type</th><th>By</th><th>Amount</th><th>Reason</th><th>Status</th><th></th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id}>
                <td>{when(row.createdAt)}</td>
                <td>{row.type}</td>
                <td>{row.requestedBy}</td>
                <td>{money(row.amount)}</td>
                <td>{row.reason}</td>
                <td><span className={row.status === 'Pending' ? 'badge warn' : 'badge'}>{row.status}</span></td>
                <td>{row.status === 'Pending' && <div className="row"><button className="btn good tiny" onClick={() => review(row.id, true).catch(error => toast(message(error), 'err'))}>Approve</button><button className="btn danger tiny" onClick={() => review(row.id, false).catch(error => toast(message(error), 'err'))}>Reject</button></div>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
