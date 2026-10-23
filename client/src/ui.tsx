import { useEffect, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { can, homeFor, useAuth } from './auth'

export function toast(text: string, kind: 'ok' | 'err' = 'ok') {
  window.dispatchEvent(new CustomEvent('saffron-toast', { detail: { text, kind } }))
}

export function Toasts() {
  const [items, setItems] = useState<{ id: number; text: string; kind: string }[]>([])
  useEffect(() => {
    const onToast = (event: Event) => {
      const detail = (event as CustomEvent<{ text: string; kind: string }>).detail
      const id = Date.now() + Math.random()
      setItems(list => [...list, { id, text: detail.text, kind: detail.kind }])
      setTimeout(() => setItems(list => list.filter(item => item.id !== id)), 4600)
    }
    window.addEventListener('saffron-toast', onToast)
    return () => window.removeEventListener('saffron-toast', onToast)
  }, [])
  return (
    <div className="toasts">
      {items.map(item => <div key={item.id} className={'toast ' + item.kind}>{item.text}</div>)}
    </div>
  )
}

const links = [
  { to: '/', label: 'Today', perm: 'reports.view' },
  { to: '/pos', label: 'Sell', perm: 'pos.order' },
  { to: '/orders', label: 'Orders', perm: 'pos.order' },
  { to: '/kitchen', label: 'Kitchen', perm: 'kitchen.view' },
  { to: '/tables', label: 'Tables', perm: 'pos.order' },
  { to: '/receipts', label: 'Receipts', perm: 'pos.order' },
  { to: '/delivery', label: 'Delivery', perm: 'delivery.manage' },
  { to: '/menu', label: 'Menu', perm: 'menu.manage' },
  { to: '/inventory', label: 'Inventory', perm: 'inventory.view' },
  { to: '/procurement', label: 'Purchasing', perm: 'purchase.manage' },
  { to: '/customers', label: 'Customers', perm: 'customer.manage' },
  { to: '/expenses', label: 'Expenses', perm: 'expense.manage' },
  { to: '/reports', label: 'Reports', perm: 'reports.view' },
  { to: '/approvals', label: 'Approvals', perm: 'approval.review' },
  { to: '/admin', label: 'Admin', perm: 'admin.settings' },
]

export function Shell({ children }: { children: React.ReactNode }) {
  const { user, branchId, setBranchId, signOut } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  if (!user) return null
  const visible = links.filter(link => can(user, link.perm) || (link.to === '/admin' && can(user, 'admin.users')))
  return (
    <div className="shell">
      <aside className={open ? 'side open' : 'side'}>
        <button className="brand" onClick={() => { navigate(homeFor(user)); setOpen(false) }}>
          <span className="mark">S</span>
          <span>
            <strong>Saffron</strong>
            <small>{user.organizationName}</small>
          </span>
        </button>
        <nav>
          {visible.map(link => (
            <NavLink key={link.to} to={link.to} end={link.to === '/'} onClick={() => setOpen(false)}>
              {link.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <section className="workspace">
        <header className="topbar">
          <button className="btn ghost menu-btn" onClick={() => setOpen(v => !v)}>Menu</button>
          <div className="who">
            <strong>{user.fullName}</strong>
            <span>{user.role}</span>
          </div>
          {user.branches.length > 0 && (
            <label className="branch-pick">
              Branch
              <select value={branchId} onChange={e => setBranchId(Number(e.target.value))}>
                {user.branches.map(branch => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
              </select>
            </label>
          )}
          <button className="btn ghost" onClick={() => { signOut(); navigate('/login') }}>Sign out</button>
        </header>
        <div className="content" onClick={() => setOpen(false)}>{children}</div>
      </section>
    </div>
  )
}

export function Modal({ title, onClose, children, wide }: { title: string; onClose: () => void; children: React.ReactNode; wide?: boolean }) {
  return (
    <div className="modal-back" onMouseDown={onClose}>
      <div className={wide ? 'modal wide' : 'modal'} onMouseDown={e => e.stopPropagation()}>
        <header>
          <h3>{title}</h3>
          <button className="btn ghost" onClick={onClose}>Close</button>
        </header>
        {children}
      </div>
    </div>
  )
}

export function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="field"><span>{label}</span>{children}</label>
}

export function Empty({ text }: { text: string }) {
  return <div className="empty">{text}</div>
}
