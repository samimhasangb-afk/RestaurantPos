import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiBase, setApiBase } from '../api'
import { homeFor, useAuth } from '../auth'
import { Field } from '../ui'

const demos = [
  ['admin', 'Admin@123'],
  ['manager', 'Manager@123'],
  ['cashier', 'Cashier@123'],
  ['waiter', 'Waiter@123'],
  ['kitchen', 'Kitchen@123'],
  ['store', 'Store@123'],
]

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('admin')
  const [password, setPassword] = useState('Admin@123')
  const [server, setServer] = useState(apiBase())
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError('')
    setApiBase(server)
    try {
      const signedIn = await signIn(username.trim(), password)
      navigate(homeFor(signedIn))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign in failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-wrap">
      <section className="hero">
        <p>Restaurant sales, kitchen, stock, and accounts</p>
        <h1>Saffron POS</h1>
        <p>Dine-in, takeaway, delivery, receipts, and branch control on one counter.</p>
      </section>
      <form className="login" onSubmit={submit}>
        <h1>Sign in</h1>
        <Field label="Username"><input value={username} onChange={e => setUsername(e.target.value)} autoFocus /></Field>
        <Field label="Password"><input type="password" value={password} onChange={e => setPassword(e.target.value)} /></Field>
        <Field label="Server address (leave blank on this PC)">
          <input value={server} placeholder="http://192.168.0.10:5088" onChange={e => setServer(e.target.value)} />
        </Field>
        {error && <div className="badge bad">{error}</div>}
        <button className="btn primary" disabled={busy}>{busy ? 'Signing in…' : 'Enter the restaurant'}</button>
        <div className="demo">
          {demos.map(([name, pass]) => (
            <button type="button" key={name} onClick={() => { setUsername(name); setPassword(pass) }}>{name}</button>
          ))}
        </div>
        <p className="muted">Demo passwords are on the chips. The API uses SQL Server database RestaurantPosDb.</p>
      </form>
    </div>
  )
}
