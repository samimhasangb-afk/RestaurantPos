import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { api, setToken, token } from './api'
import type { User } from './types'

type Session = {
  user: User | null
  ready: boolean
  branchId: number
  setBranchId: (id: number) => void
  signIn: (username: string, password: string) => Promise<User>
  signOut: () => void
  refresh: () => Promise<void>
}

const Ctx = createContext<Session | null>(null)
const BRANCH = 'saffron.branch'

export function can(user: User | null, perm: string) {
  if (!user) return false
  if (user.role === 'Admin') return true
  return user.permissions.includes(perm)
}

export function homeFor(user: User) {
  if (can(user, 'reports.view')) return '/'
  if (can(user, 'pos.order')) return '/pos'
  if (can(user, 'kitchen.view')) return '/kitchen'
  if (can(user, 'inventory.view')) return '/inventory'
  return '/pos'
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [ready, setReady] = useState(false)
  const [branchId, setBranchState] = useState(Number(localStorage.getItem(BRANCH) || 0))

  function setBranchId(id: number) {
    localStorage.setItem(BRANCH, String(id))
    setBranchState(id)
  }

  async function refresh() {
    if (!token()) {
      setUser(null)
      return
    }
    const me = await api<User>('/api/auth/me')
    setUser(me)
    const saved = Number(localStorage.getItem(BRANCH) || 0)
    const allowed = me.branches.some(b => b.id === saved)
    const next = allowed ? saved : me.branchId || me.branches[0]?.id || 0
    if (next) setBranchId(next)
  }

  useEffect(() => {
    refresh().catch(() => setUser(null)).finally(() => setReady(true))
  }, [])

  async function signIn(username: string, password: string) {
    const res = await api<{ token: string; user: User }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    })
    setToken(res.token)
    setUser(res.user)
    const next = res.user.branchId || res.user.branches[0]?.id || 0
    if (next) setBranchId(next)
    return res.user
  }

  function signOut() {
    setToken('')
    setUser(null)
  }

  const value = useMemo(() => ({ user, ready, branchId, setBranchId, signIn, signOut, refresh }), [user, ready, branchId])
  return <Ctx.Provider value={value}>{children}</Ctx.Provider>
}

export function useAuth() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('Auth missing')
  return ctx
}
