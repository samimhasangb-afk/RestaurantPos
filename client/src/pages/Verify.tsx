import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api, message, money, when } from '../api'
import type { Verify } from '../types'

export function VerifyPage() {
  const { token } = useParams()
  const [data, setData] = useState<Verify | null>(null)
  const [error, setError] = useState('')
  useEffect(() => {
    api<Verify>('/api/public/receipts/' + token).then(setData).catch(err => setError(message(err)))
  }, [token])
  return (
    <div className="verify card">
      <h1>Receipt check</h1>
      {error && <p className="badge bad">{error}</p>}
      {data && (
        <>
          <p className={data.valid ? 'badge good' : 'badge bad'}>{data.message}</p>
          <p>{data.branch}</p>
          <p>Receipt {data.receiptNo} · Order {data.orderNo}</p>
          <p>{when(data.createdAt)} · {data.paymentStatus} · printed {data.printCount}</p>
          {data.lines.map((line, index) => <div className="tline" key={index}><span>{line.qty} × {line.name}</span><b>{money(line.lineTotal)}</b></div>)}
          <h2>{money(data.grandTotal)}</h2>
        </>
      )}
    </div>
  )
}
