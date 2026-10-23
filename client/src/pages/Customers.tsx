import { useEffect, useState } from 'react'
import { api, message, money } from '../api'
import { Field, toast } from '../ui'
import type { Customer } from '../types'

export function CustomersPage() {
  const [rows, setRows] = useState<Customer[]>([])
  const [form, setForm] = useState({ name: '', phone: '', address: '' })
  function load() { api<Customer[]>('/api/customers').then(setRows).catch(error => toast(message(error), 'err')) }
  useEffect(() => { load() }, [])
  return (
    <div>
      <div className="page-head"><div><h1>Customers</h1><p>House accounts and due collection.</p></div></div>
      <div className="card table-wrap">
        <table>
          <thead><tr><th>Name</th><th>Phone</th><th>Due</th><th></th></tr></thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.id}>
                <td>{row.name}<div className="muted">{row.address}</div></td>
                <td>{row.phone}</td>
                <td>{money(row.dueBalance)}</td>
                <td>{row.dueBalance > 0 && <button className="btn ghost tiny" onClick={() => { const amount = Number(prompt('Collect amount') || 0); if (amount > 0) api(`/api/customers/${row.id}/settle`, { method: 'POST', body: JSON.stringify({ amount, method: 'Cash' }) }).then(load).catch(error => toast(message(error), 'err')) }}>Collect</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="card grid" style={{ maxWidth: 480, marginTop: 12 }}>
        <Field label="Name"><input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Phone"><input value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} /></Field>
        <Field label="Address"><input value={form.address} onChange={e => setForm({ ...form, address: e.target.value })} /></Field>
        <button className="btn primary" onClick={() => api('/api/customers', { method: 'POST', body: JSON.stringify({ ...form, isActive: true }) }).then(() => { toast('Customer saved'); setForm({ name: '', phone: '', address: '' }); load() }).catch(error => toast(message(error), 'err'))}>Save customer</button>
      </div>
    </div>
  )
}
