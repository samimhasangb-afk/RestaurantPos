const TOKEN = 'saffron.token'
const BASE = 'saffron.apiBase'

export function apiBase() {
  const saved = localStorage.getItem(BASE)
  if (saved !== null && saved !== '') return saved.replace(/\/$/, '')
  if (location.hostname === 'localhost' || location.hostname === '127.0.0.1') return ''
  return `http://${location.hostname}:5088`
}

export function setApiBase(value: string) {
  localStorage.setItem(BASE, value.trim().replace(/\/$/, ''))
}

export function token() {
  return localStorage.getItem(TOKEN) || ''
}

export function setToken(value: string) {
  if (value) localStorage.setItem(TOKEN, value)
  else localStorage.removeItem(TOKEN)
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  if (init?.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const auth = token()
  if (auth) headers.set('Authorization', 'Bearer ' + auth)
  let res: Response
  try {
    res = await fetch(apiBase() + path, { ...init, headers })
  } catch {
    throw new ApiError('Cannot reach the POS server. Check that the API is running.', 0)
  }
  if (res.status === 401 && !path.includes('/auth/login')) {
    setToken('')
    if (!location.hash.includes('/login')) location.hash = '#/login'
  }
  const text = await res.text()
  const data = text ? JSON.parse(text) as { message?: string } : null
  if (!res.ok) throw new ApiError(data?.message || 'Request failed.', res.status)
  return data as T
}

export async function openReceipt(orderId: number, width = 80) {
  const headers = new Headers()
  const auth = token()
  if (auth) headers.set('Authorization', 'Bearer ' + auth)
  const res = await fetch(`${apiBase()}/api/orders/${orderId}/receipt?width=${width}`, { headers })
  const html = await res.text()
  if (!res.ok) {
    let message = 'Could not print the receipt.'
    try { message = (JSON.parse(html) as { message?: string }).message || message } catch { /* html error page */ }
    throw new ApiError(message, res.status)
  }
  const popup = window.open('', '_blank', 'width=420,height=760')
  if (!popup) throw new ApiError('Allow pop-ups to print the receipt.', 0)
  popup.document.open()
  popup.document.write(html)
  popup.document.close()
}

export function money(value?: number | null) {
  const amount = Number(value || 0)
  return '৳' + amount.toLocaleString('en-BD', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function when(value?: string | null) {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-GB', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })
}

export function todayInput() {
  const date = new Date()
  const z = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${z(date.getMonth() + 1)}-${z(date.getDate())}`
}

export function qty(value: number) {
  return Number(value).toLocaleString('en-BD', { maximumFractionDigits: 3 })
}

export function message(error: unknown) {
  return error instanceof Error ? error.message : 'Something went wrong'
}
