import { useEffect, useState } from 'react'
import { api, message, when } from '../api'
import { Field, toast } from '../ui'
import type { Audit, Branch, LoginRow, Monitor, Role, Setting, Terminal, User } from '../types'

export function AdminPage() {
  const [tab, setTab] = useState('users')
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [branches, setBranches] = useState<Branch[]>([])
  const [terminals, setTerminals] = useState<Terminal[]>([])
  const [audit, setAudit] = useState<Audit[]>([])
  const [logins, setLogins] = useState<LoginRow[]>([])
  const [settings, setSettings] = useState<Setting[]>([])
  const [monitor, setMonitor] = useState<Monitor | null>(null)
  const [form, setForm] = useState({ fullName: '', username: '', password: '', roleId: 0, defaultBranchId: 0, branchIds: [] as number[] })

  function load() {
    api<User[]>('/api/admin/users').then(setUsers).catch(error => toast(message(error), 'err'))
    api<Role[]>('/api/admin/roles').then(setRoles).catch(() => undefined)
    api<Branch[]>('/api/admin/branches').then(setBranches).catch(() => undefined)
    api<Terminal[]>('/api/admin/terminals').then(setTerminals).catch(() => undefined)
    api<Audit[]>('/api/admin/audit').then(setAudit).catch(() => undefined)
    api<LoginRow[]>('/api/admin/logins').then(setLogins).catch(() => undefined)
    api<Setting[]>('/api/admin/settings').then(setSettings).catch(() => undefined)
    api<Monitor>('/api/admin/monitor').then(setMonitor).catch(() => undefined)
  }
  useEffect(() => { load() }, [])

  return (
    <div>
      <div className="page-head"><div><h1>Admin</h1><p>People, branches, audit, settings, backup, and health.</p></div></div>
      <div className="chips">{['users', 'branches', 'audit', 'settings', 'backup'].map(item => <button key={item} className={tab === item ? 'chip on' : 'chip'} onClick={() => setTab(item)}>{item}</button>)}</div>
      {tab === 'users' && (
        <div className="grid" style={{ gridTemplateColumns: '1fr .8fr' }}>
          <div className="card table-wrap">
            <table>
              <thead><tr><th>Name</th><th>Username</th><th>Role</th><th>Branches</th></tr></thead>
              <tbody>{users.map(user => <tr key={user.id}><td>{user.fullName}</td><td>{user.username}</td><td>{user.role}</td><td>{user.branches.map(b => b.name).join(', ')}</td></tr>)}</tbody>
            </table>
          </div>
          <div className="card grid">
            <Field label="Full name"><input value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} /></Field>
            <Field label="Username"><input value={form.username} onChange={e => setForm({ ...form, username: e.target.value })} /></Field>
            <Field label="Password"><input value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} /></Field>
            <Field label="Role"><select value={form.roleId} onChange={e => setForm({ ...form, roleId: Number(e.target.value) })}><option value={0}>Choose</option>{roles.map(role => <option key={role.id} value={role.id}>{role.name}</option>)}</select></Field>
            <Field label="Home branch"><select value={form.defaultBranchId} onChange={e => setForm({ ...form, defaultBranchId: Number(e.target.value), branchIds: [Number(e.target.value)] })}><option value={0}>Choose</option>{branches.map(branch => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select></Field>
            <button className="btn primary" onClick={() => api('/api/admin/users', { method: 'POST', body: JSON.stringify({ ...form, isActive: true, branchIds: form.branchIds.filter(Boolean) }) }).then(() => { toast('User saved'); load() }).catch(error => toast(message(error), 'err'))}>Save user</button>
          </div>
        </div>
      )}
      {tab === 'branches' && (
        <div className="card">
          {branches.map(branch => <p key={branch.id}><b>{branch.name}</b> ({branch.code}) · tax {branch.taxPercent}% · service {branch.serviceChargePercent}% · {branch.address}</p>)}
          <h3>Terminals</h3>
          {terminals.map(terminal => <p key={terminal.id}>{terminal.name}</p>)}
        </div>
      )}
      {tab === 'audit' && (
        <div className="grid">
          <div className="card table-wrap">
            <h3>Activity</h3>
            <table><tbody>{audit.map(row => <tr key={row.id}><td>{when(row.createdAt)}</td><td>{row.userName}</td><td>{row.action}</td><td>{row.detail}</td></tr>)}</tbody></table>
          </div>
          <div className="card table-wrap">
            <h3>Sign-in history</h3>
            <table><tbody>{logins.map(row => <tr key={row.id}><td>{when(row.createdAt)}</td><td>{row.username}</td><td>{row.success ? 'Success' : 'Failed'}</td><td>{row.ip}</td></tr>)}</tbody></table>
          </div>
        </div>
      )}
      {tab === 'settings' && (
        <div className="card grid">
          {settings.map((setting, index) => (
            <Field key={setting.key} label={setting.key}>
              <input value={setting.value} onChange={e => setSettings(current => current.map((item, i) => i === index ? { ...item, value: e.target.value } : item))} />
            </Field>
          ))}
          <button className="btn primary" onClick={() => api('/api/admin/settings', { method: 'PUT', body: JSON.stringify(settings) }).then(() => toast('Settings saved')).catch(error => toast(message(error), 'err'))}>Save settings</button>
        </div>
      )}
      {tab === 'backup' && (
        <div className="card">
          {monitor && <p>Database {monitor.databaseOnline ? 'online' : 'offline'} · users {monitor.users} · open orders {monitor.openOrders} · low stock {monitor.lowStock}</p>}
          <button className="btn" onClick={() => api<{ message: string }>('/api/admin/backup', { method: 'POST' }).then(res => { toast(res.message); load() }).catch(error => toast(message(error), 'err'))}>Create backup</button>
          <ul>{(monitor?.backups || []).map(file => <li key={file}>{file}</li>)}</ul>
        </div>
      )}
    </div>
  )
}
