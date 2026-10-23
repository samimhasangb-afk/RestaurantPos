import { useEffect, useState } from 'react'
import { api, message, money, todayInput, when } from '../api'
import { useAuth } from '../auth'
import { Field, toast } from '../ui'
import type { Expense } from '../types'

export function ExpensesPage() {
  const { branchId } = useAuth()
  const [rows, setRows] = useState<Expense[]>([])
  const [form, setForm] = useState({ category: 'Kitchen', amount: '', note: '', spentAt: todayInput() })
  function load() { api<Expense[]>(`/api/expenses?branchId=${branchId}`).then(setRows).catch(error => toast(message(error), 'err')) }
  useEffect(() => { load() }, [branchId])
  return (
    <div>
      <div className="page-head"><div><h1>Expenses</h1><p>Cash leaving the branch outside of supplier bills.</p></div></div>
      <div className="card grid" style={{ maxWidth: 520, marginBottom: 12 }}>
        <Field label="Category"><input value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} /></Field>
        <Field label="Amount"><input value={form.amount} onChange={e => setForm({ ...form, amount: e.target.value })} /></Field>
        <Field label="Note"><input value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} /></Field>
        <button className="btn primary" onClick={() => api('/api/expenses', { method: 'POST', body: JSON.stringify({ branchId, category: form.category, amount: Number(form.amount), note: form.note }) }).then(() => { toast('Expense saved'); load() }).catch(error => toast(message(error), 'err'))}>Save expense</button>
      </div>
      <div className="card table-wrap">
        <table>
          <thead><tr><th>When</th><th>Branch</th><th>Category</th><th>Amount</th><th>Note</th></tr></thead>
          <tbody>{rows.map(row => <tr key={row.id}><td>{when(row.spentAt)}</td><td>{row.branchName}</td><td>{row.category}</td><td>{money(row.amount)}</td><td>{row.note}</td></tr>)}</tbody>
        </table>
      </div>
    </div>
  )
}
