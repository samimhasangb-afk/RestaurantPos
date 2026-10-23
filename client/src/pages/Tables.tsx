import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, message } from '../api'
import { useAuth } from '../auth'
import { Field, toast } from '../ui'
import type { Catalog } from '../types'

export function TablesPage() {
  const { branchId } = useAuth()
  const navigate = useNavigate()
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [name, setName] = useState('')
  const [seats, setSeats] = useState('4')
  const [floorId, setFloorId] = useState(0)
  useEffect(() => { api<Catalog>(`/api/catalog?branchId=${branchId}`).then(setCatalog).catch(error => toast(message(error), 'err')) }, [branchId])
  const floors = catalog?.floors || []
  return (
    <div>
      <div className="page-head"><div><h1>Tables</h1><p>Free tables open a new dine-in ticket. Occupied tables resume the open check.</p></div></div>
      {floors.map(floor => (
        <section key={floor.id} style={{ marginBottom: 16 }}>
          <h3>{floor.name}</h3>
          <div className="tables">
            {catalog?.tables.filter(table => table.floorId === floor.id).map(table => (
              <button key={table.id} className={'table-tile ' + table.status} onClick={() => navigate(table.openOrderId ? `/orders` : `/pos?table=${table.id}`)}>
                <strong>{table.name}</strong>
                <div>{table.seats} seats</div>
                <span className="badge">{table.status}</span>
              </button>
            ))}
          </div>
        </section>
      ))}
      <div className="card grid" style={{ maxWidth: 480 }}>
        <h3>Add a table</h3>
        <Field label="Floor"><select value={floorId || floors[0]?.id || ''} onChange={e => setFloorId(Number(e.target.value))}>{floors.map(floor => <option key={floor.id} value={floor.id}>{floor.name}</option>)}</select></Field>
        <Field label="Name"><input value={name} onChange={e => setName(e.target.value)} /></Field>
        <Field label="Seats"><input value={seats} onChange={e => setSeats(e.target.value)} /></Field>
        <button className="btn" onClick={() => api('/api/tables', { method: 'POST', body: JSON.stringify({ branchId, floorId: floorId || floors[0]?.id, name, seats: Number(seats) }) }).then(() => api<Catalog>(`/api/catalog?branchId=${branchId}`)).then(setCatalog).catch(error => toast(message(error), 'err'))}>Save table</button>
      </div>
    </div>
  )
}
