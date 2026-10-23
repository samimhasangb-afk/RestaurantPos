import { useEffect, useState } from 'react'
import { api, message, when } from '../api'
import { useAuth } from '../auth'
import { Empty, toast } from '../ui'
import type { KitchenCard } from '../types'

const next: Record<string, string> = { Pending: 'Preparing', Preparing: 'Ready', Ready: 'Served', Served: 'Served' }

export function KitchenPage() {
  const { branchId } = useAuth()
  const [cards, setCards] = useState<KitchenCard[]>([])
  function load() {
    api<KitchenCard[]>(`/api/kitchen?branchId=${branchId}`).then(setCards).catch(error => toast(message(error), 'err'))
  }
  useEffect(() => { load(); const timer = setInterval(load, 8000); return () => clearInterval(timer) }, [branchId])

  async function bump(lineId: number, status: string) {
    await api(`/api/kitchen/lines/${lineId}`, { method: 'POST', body: JSON.stringify({ status: next[status] || 'Ready' }) })
    load()
  }

  return (
    <div>
      <div className="page-head"><div><h1>Kitchen</h1><p>Course tickets update as the pass moves.</p></div><button className="btn ghost" onClick={load}>Refresh</button></div>
      {cards.length === 0 && <Empty text="The pass is clear." />}
      <div className="kds">
        {cards.map(card => (
          <article key={card.ticketId} className={'kcard ' + card.status}>
            <div className="row" style={{ justifyContent: 'space-between' }}><strong>{card.ticketNo}</strong><span className="badge">{card.course}</span></div>
            <p className="muted">{card.orderNo} · {card.tableName || card.orderType} · {when(card.createdAt)}</p>
            {card.lines.map(line => (
              <button key={line.saleLineId} className="choice" onClick={() => bump(line.saleLineId, line.prepStatus).catch(error => toast(message(error), 'err'))}>
                <span>{line.qty} × {line.name}<small style={{ display: 'block' }}>{line.notes}</small></span>
                <b>{line.prepStatus}</b>
              </button>
            ))}
          </article>
        ))}
      </div>
    </div>
  )
}
